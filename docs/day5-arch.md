# Architecture

## High-Level Diagram
```mermaid
flowchart TD
  subgraph Clients
    C1[Backend Clients]
    C2[Partner Systems]
  end
  C1 -->|HTTPS| APIGW[API Gateway\n(Usage Plans, API Keys)]
  C2 -->|HTTPS| APIGW
  APIGW --> ALB[ALB]
  ALB --> API[EKS: API Service\n(.NET)]
  subgraph Data Plane
    REDIS[(Redis\nRate Limit + Idempotency Cache)]
    RDS[(RDS Postgres\nTenants, Audit, Outbox, Processed)]
    DDB[(DynamoDB\nNotification Status)]
    S3[(S3\nAudit/Cold Storage)]
    OS[(OpenSearch/CloudWatch\nLogs)]
    KMS[(KMS)]
  end
  API --> REDIS
  API --> RDS
  API -->|Status Writes| DDB
  API -->|Bulk/Archive| S3
  API --> OS
  API --> KMS
  API --> SQS[(SQS\nOutbox Queue)]
  subgraph Workers
    WK1[EKS Worker(s)\nDispatcher]
  end
  SQS --> WK1
  WK1 -->|Idempotent Guard| RDS
  WK1 -->|Update Status| DDB
  WK1 --> OS
  subgraph Providers
    SNS[SNS/SMS]
    SES[SES/Email]
    PUSH[FCM/APNs]
  end
  WK1 --> SNS
  WK1 --> SES
  WK1 --> PUSH
  subgraph Callbacks
    PB[Provider Callbacks\n(Webhooks)]
  end
  SNS -.-> PB
  SES -.-> PB
  PUSH -.-> PB
  PB --> API
  API -->|Status Update| DDB
  classDef storage fill:#eef,stroke:#88a;
  class RDS,REDIS,DDB,S3 storage;
sequenceDiagram
  autonumber
  participant CL as Client
  participant GW as API Gateway
  participant API as API (EKS)
  participant R as Redis
  participant DB as RDS
  participant Q as SQS
  CL->>GW: POST /v1/notifications (Idempotency-Key, X-Tenant-Id)
  GW->>API: Forward
  API->>R: GET idem:{tenant}:{key}
  alt Duplicate
    R-->>API: hit {notificationId}
    API-->>CL: 202 {notificationId} (cached)
  else New
    R-->>API: miss
    API->>R: RateLimit try_consume
    alt Allowed
      API->>DB: TXN audit+outbox
      API->>Q: Enqueue {messageId, payload, trace}
      API-->>CL: 202 {notificationId}
      API->>R: SET idem:{tenant}:{key} -> {notificationId} TTL 24h
    else Throttled
      API-->>CL: 429
    end
  end
sequenceDiagram
  autonumber
  participant Q as SQS
  participant WK as Worker
  participant DB as RDS
  participant P as Provider
  participant D as DynamoDB
  loop Poll
    Q-->>WK: Receive {messageId, payload}
    WK->>DB: TryInsert processed(message_id)
    alt First time
      DB-->>WK: ok
      rect rgb(245,245,245)
        WK->>P: Send
        alt Success
          P-->>WK: 2xx
          WK->>D: status=SENT
          WK->>DB: audit(SENT)
          WK-->>Q: Delete
        else Retriable
          P-->>WK: 5xx/timeout
          WK->>DB: outbox attempt+1; NextAttemptUtc
          WK-->>Q: Release
        end
      end
    else Duplicate
      DB-->>WK: exists
      WK-->>Q: Delete
    end
  end
sequenceDiagram
  autonumber
  participant CL as Client
  participant GW as API Gateway
  participant API as API (EKS)
  participant D as DynamoDB
  CL->>GW: GET /v1/notifications/{id}/status
  GW->>API: Forward
  API->>D: GetItem tenant#id
  alt Found
    D-->>API: {state,lastUpdate}
    API-->>CL: 200 {state,lastUpdate}
  else Missing
    API-->>CL: 404
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
