# Architecture

## High-Level Diagram
```mermaid
flowchart TD
  subgraph Clients
    C1[Backend Clients]
    C2[Partner Systems]
  end

  C1 -->|HTTPS| APIGW["API Gateway<br/>(Usage Plans, API Keys)"]
  C2 -->|HTTPS| APIGW

  APIGW --> ALB[ALB]
  ALB --> API["EKS: API Service<br/>(.NET)"]

  subgraph "Data Plane"
    REDIS["Redis<br/>(Rate Limit + Idempotency Cache)"]
    RDS["RDS Postgres<br/>(Tenants, Audit, Outbox, Processed)"]
    DDB["DynamoDB<br/>(Notification Status)"]
    S3["S3<br/>(Audit/Cold Storage)"]
    OS["OpenSearch/CloudWatch<br/>(Logs)"]
    KMS[KMS]
  end

  API --> REDIS
  API --> RDS
  API -->|Status Writes| DDB
  API -->|Bulk/Archive| S3
  API --> OS
  API --> KMS

  API --> SQS["SQS<br/>(Outbox Queue)"]

  subgraph Workers
    WK1["EKS Worker(s) Dispatcher"]
  end

  SQS --> WK1
  WK1 -->|Idempotent Guard| RDS
  WK1 -->|Update Status| DDB
  WK1 --> OS

  subgraph Providers
    SNS["SNS/SMS"]
    SES["SES/Email"]
    PUSH["FCM/APNs"]
  end

  WK1 --> SNS
  WK1 --> SES
  WK1 --> PUSH

  subgraph Callbacks
    PB["Provider Callbacks (Webhooks)"]
  end

  SNS -.-> PB
  SES -.-> PB
  PUSH -.-> PB
  PB --> API
  API -->|Status Update| DDB

  classDef storage fill:#eef,stroke:#88a;
  class RDS,REDIS,DDB,S3 storage;
sequenceDiagram
  participant CL as Client
  participant GW as "API Gateway"
  participant API as "API (EKS)"
  participant R as Redis
  participant DB as RDS
  participant Q as SQS

  CL->>GW: POST /v1/notifications (Idempotency-Key, X-Tenant-Id)
  GW->>API: Forward request
  API->>R: Check idem:{tenant}:{key}
  alt Duplicate
    R-->>API: cache hit (notificationId)
    API-->>CL: 202 Accepted (cached id)
  else New
    R-->>API: miss
    API->>DB: TXN audit + outbox
    API->>Q: Enqueue message
    API-->>CL: 202 Accepted (notificationId)
    API->>R: Store idem key with TTL
  end
sequenceDiagram
  participant Q as SQS
  participant WK as Worker
  participant DB as RDS
  participant P as "Provider"
  participant D as DynamoDB

  Q-->>WK: Receive message
  WK->>DB: Try insert processed(message_id)
  alt First-time
    DB-->>WK: ok
    WK->>P: Send notification
    alt Success
      P-->>WK: 2xx
      WK->>D: status = SENT
      WK->>DB: audit(SENT)
      WK-->>Q: Delete message
    else Retriable error
      P-->>WK: 5xx/timeout
      WK->>DB: bump attempts / schedule next
      WK-->>Q: Release (visibility timeout)
    end
  else Duplicate
    DB-->>WK: already exists
    WK-->>Q: Delete message (skip)
  end
sequenceDiagram
  participant CL as Client
  participant GW as "API Gateway"
  participant API as "API (EKS)"
  participant D as DynamoDB

  CL->>GW: GET /v1/notifications/{id}/status
  GW->>API: Forward
  API->>D: GetItem tenant#id
  alt Found
    D-->>API: {state,lastUpdate}
    API-->>CL: 200 {state,lastUpdate}
  else Missing
    API-->>CL: 404 Not Found
  end
stateDiagram-v2
  [*] --> QUEUED
  QUEUED --> SENDING : worker dequeues
  SENDING --> SENT : provider 2xx
  SENDING --> FAILED : permanent error
  SENDING --> RETRYING : transient error
  RETRYING --> SENDING : backoff window
  RETRYING --> POISONED : max attempts exceeded
  SENT --> DELIVERED : provider callback (optional)
  POISONED --> [*]
  DELIVERED --> [*]
  FAILED --> [*]
