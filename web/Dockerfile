# web/Dockerfile
FROM node:20-alpine AS build
WORKDIR /app

# copy package files and install dependencies
COPY web/package*.json ./
RUN npm ci

# copy the web app sources (copy contents into /app) and build
COPY web ./
RUN npm run build

FROM nginx:alpine

# copy built static assets
COPY --from=build /app/dist /usr/share/nginx/html

# write a valid nginx config (note space in "location = /healthz")
RUN cat > /etc/nginx/nginx.conf <<'NGX'
user nginx;
worker_processes auto;

events {
  worker_connections 1024;
}

http {
  server {
    listen 80;
    root /usr/share/nginx/html;

    # exact-match health check
    location = /healthz {
      return 200 'ok';
      add_header Content-Type text/plain;
    }

    # SPA fallback
    location / {
      try_files $uri $uri/ /index.html;
    }
  }
}
NGX

# ensure wget is available for the HEALTHCHECK
RUN apk add --no-cache wget ca-certificates

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
  CMD wget -qO- http://127.0.0.1/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
