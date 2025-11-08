# Markdn Deployment Guide

Complete deployment guide for running Markdn in production environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Cloud Platforms](#cloud-platforms)
- [Reverse Proxy Setup](#reverse-proxy-setup)
- [Production Checklist](#production-checklist)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

- .NET 8.0 Runtime (for native deployment)
- Docker (for containerized deployment)
- Kubernetes cluster (for k8s deployment)
- Domain name with DNS configured
- SSL/TLS certificate (Let's Encrypt recommended)

---

## Docker Deployment

### Dockerfile

Create a `Dockerfile` in the repository root:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Markdn.Api/Markdn.Api.csproj", "src/Markdn.Api/"]
RUN dotnet restore "src/Markdn.Api/Markdn.Api.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/src/Markdn.Api"
RUN dotnet build "Markdn.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Markdn.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create content directory
RUN mkdir -p /app/content

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Markdn.Api.dll"]
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  markdn:
    build: .
    container_name: markdn
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - Markdn__ContentDirectory=/app/content
      - Markdn__MaxFileSizeBytes=5242880
      - Markdn__DefaultPageSize=50
    volumes:
      - ./content:/app/content:ro
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

### Build and Run

```bash
# Build the image
docker build -t markdn:latest .

# Run with docker-compose
docker-compose up -d

# Check logs
docker-compose logs -f

# Stop
docker-compose down
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Production |
| `ASPNETCORE_URLS` | Listening URLs | http://+:8080 |
| `Markdn__ContentDirectory` | Content directory path | /app/content |
| `Markdn__MaxFileSizeBytes` | Max file size | 5242880 (5MB) |
| `Markdn__DefaultPageSize` | Default page size | 50 |

---

## Kubernetes Deployment

### Namespace

```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: markdn
```

### ConfigMap

```yaml
# configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: markdn-config
  namespace: markdn
data:
  appsettings.json: |
    {
      "Markdn": {
        "ContentDirectory": "/app/content",
        "MaxFileSizeBytes": 5242880,
        "DefaultPageSize": 50
      }
    }
```

### PersistentVolume (for content)

```yaml
# pv-content.yaml
apiVersion: v1
kind: PersistentVolume
metadata:
  name: markdn-content-pv
spec:
  capacity:
    storage: 10Gi
  accessModes:
    - ReadOnlyMany
  hostPath:
    path: /mnt/markdn-content
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: markdn-content-pvc
  namespace: markdn
spec:
  accessModes:
    - ReadOnlyMany
  resources:
    requests:
      storage: 10Gi
```

### Deployment

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: markdn
  namespace: markdn
spec:
  replicas: 3
  selector:
    matchLabels:
      app: markdn
  template:
    metadata:
      labels:
        app: markdn
    spec:
      containers:
      - name: markdn
        image: markdn:latest
        ports:
        - containerPort: 8080
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        volumeMounts:
        - name: content
          mountPath: /app/content
          readOnly: true
        - name: config
          mountPath: /app/appsettings.Production.json
          subPath: appsettings.json
          readOnly: true
        livenessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
      volumes:
      - name: content
        persistentVolumeClaim:
          claimName: markdn-content-pvc
      - name: config
        configMap:
          name: markdn-config
```

### Service

```yaml
# service.yaml
apiVersion: v1
kind: Service
metadata:
  name: markdn
  namespace: markdn
spec:
  selector:
    app: markdn
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: ClusterIP
```

### Ingress (with TLS)

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: markdn
  namespace: markdn
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.yourdomain.com
    secretName: markdn-tls
  rules:
  - host: api.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: markdn
            port:
              number: 80
```

### Deploy to Kubernetes

```bash
# Apply all configs
kubectl apply -f namespace.yaml
kubectl apply -f configmap.yaml
kubectl apply -f pv-content.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f ingress.yaml

# Check status
kubectl get pods -n markdn
kubectl get svc -n markdn
kubectl get ingress -n markdn

# View logs
kubectl logs -f -n markdn -l app=markdn

# Scale
kubectl scale deployment markdn -n markdn --replicas=5
```

---

## Cloud Platforms

### Azure App Service

1. **Create App Service:**
```bash
az webapp create \
  --resource-group markdn-rg \
  --plan markdn-plan \
  --name markdn-api \
  --runtime "DOTNETCORE:8.0"
```

2. **Configure Settings:**
```bash
az webapp config appsettings set \
  --resource-group markdn-rg \
  --name markdn-api \
  --settings \
    Markdn__ContentDirectory=/home/content \
    Markdn__MaxFileSizeBytes=5242880 \
    Markdn__DefaultPageSize=50
```

3. **Deploy:**
```bash
dotnet publish -c Release
cd src/Markdn.Api/bin/Release/net8.0/publish
az webapp deployment source config-zip \
  --resource-group markdn-rg \
  --name markdn-api \
  --src markdn.zip
```

### AWS Elastic Beanstalk

1. **Install EB CLI:**
```bash
pip install awsebcli
```

2. **Initialize:**
```bash
eb init -p "64bit Amazon Linux 2 v2.5.0 running .NET Core" markdn
```

3. **Create Environment:**
```bash
eb create markdn-prod \
  --instance-type t3.small \
  --envvars Markdn__ContentDirectory=/var/app/content
```

4. **Deploy:**
```bash
dotnet publish -c Release
eb deploy
```

### Google Cloud Run

1. **Build and Push Image:**
```bash
gcloud builds submit --tag gcr.io/PROJECT_ID/markdn
```

2. **Deploy:**
```bash
gcloud run deploy markdn \
  --image gcr.io/PROJECT_ID/markdn \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars Markdn__ContentDirectory=/app/content
```

---

## Reverse Proxy Setup

### Nginx

```nginx
# /etc/nginx/sites-available/markdn
upstream markdn {
    server localhost:8080;
}

server {
    listen 80;
    server_name api.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Security headers
    add_header X-Content-Type-Options nosniff;
    add_header X-Frame-Options DENY;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Caching
    location ~* \.(jpg|jpeg|png|gif|ico|css|js)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # API requests
    location / {
        proxy_pass http://markdn;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # Rate limiting
        limit_req zone=api burst=20 nodelay;
    }
}

# Rate limiting zone
limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
```

Enable and test:

```bash
sudo ln -s /etc/nginx/sites-available/markdn /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Apache

```apache
# /etc/apache2/sites-available/markdn.conf
<VirtualHost *:80>
    ServerName api.yourdomain.com
    Redirect permanent / https://api.yourdomain.com/
</VirtualHost>

<VirtualHost *:443>
    ServerName api.yourdomain.com

    SSLEngine on
    SSLCertificateFile /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem
    SSLCertificateKeyFile /etc/letsencrypt/live/api.yourdomain.com/privkey.pem

    ProxyPreserveHost On
    ProxyPass / http://localhost:8080/
    ProxyPassReverse / http://localhost:8080/

    # Security headers
    Header always set X-Content-Type-Options nosniff
    Header always set X-Frame-Options DENY
    Header always set X-XSS-Protection "1; mode=block"
    Header always set Strict-Transport-Security "max-age=31536000; includeSubDomains"
</VirtualHost>
```

Enable and restart:

```bash
sudo a2enmod ssl proxy proxy_http headers
sudo a2ensite markdn
sudo systemctl reload apache2
```

---

## Production Checklist

### Security

- [ ] HTTPS enabled with valid SSL certificate
- [ ] Security headers configured (via reverse proxy or app)
- [ ] Rate limiting enabled
- [ ] Content directory is read-only for the application
- [ ] File upload disabled (if not needed)
- [ ] Secrets managed via environment variables or key vault
- [ ] Regular security updates applied

### Performance

- [ ] Reverse proxy caching configured
- [ ] CDN setup for static content
- [ ] Appropriate resource limits set (CPU/memory)
- [ ] Content directory on fast storage (SSD)
- [ ] Database/cache (if extended) properly indexed

### Reliability

- [ ] Health checks configured
- [ ] Auto-restart on failure
- [ ] Multiple replicas/instances (for high availability)
- [ ] Backups of content directory scheduled
- [ ] Monitoring and alerting enabled
- [ ] Log aggregation configured

### Operations

- [ ] CI/CD pipeline setup
- [ ] Blue-green or rolling deployment strategy
- [ ] Rollback procedure documented
- [ ] Runbook for common issues
- [ ] On-call rotation defined

---

## Monitoring

### Prometheus + Grafana

Add metrics endpoint to `Program.cs`:

```csharp
using Prometheus;

// Add metrics
app.UseHttpMetrics();
app.MapMetrics();
```

Prometheus config:

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'markdn'
    static_configs:
      - targets: ['localhost:8080']
    metrics_path: '/metrics'
```

### Application Insights (Azure)

Install package:

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

Configure in `Program.cs`:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### CloudWatch (AWS)

Install AWS SDK:

```bash
dotnet add package AWSSDK.CloudWatchLogs
```

Configure logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "AWS.Logging": {
      "Region": "us-east-1",
      "LogGroup": "markdn"
    }
  }
}
```

---

## Troubleshooting

### Application Won't Start

Check logs:
```bash
# Docker
docker-compose logs -f

# Kubernetes
kubectl logs -f -n markdn -l app=markdn

# Systemd
sudo journalctl -u markdn -f
```

Common issues:
- Content directory doesn't exist or has wrong permissions
- Port already in use
- Invalid configuration

### High Memory Usage

1. Check file sizes: `find content -type f -size +5M`
2. Reduce `MaxFileSizeBytes` in config
3. Monitor with: `docker stats` or `kubectl top pods`

### Slow Response Times

1. Check content directory performance: `iostat -x 1`
2. Enable response compression in `Program.cs`:
```csharp
builder.Services.AddResponseCompression();
app.UseResponseCompression();
```
3. Add caching at reverse proxy level
4. Profile with: `dotnet-trace collect -p PID`

### 404 Errors

- Verify content files exist: `ls -la content/`
- Check slug generation: Enable debug logging
- Verify file permissions: `chmod -R a+r content/`

### Container Crashes

Check resource limits:
```bash
# Docker
docker stats

# Kubernetes
kubectl describe pod <pod-name> -n markdn
kubectl top pod <pod-name> -n markdn
```

Increase limits if needed.

---

## Maintenance

### Updating Content

Content directory is read at runtime. To update:

```bash
# Sync new content
rsync -av --delete local-content/ server:/app/content/

# No restart needed - changes picked up on next request
```

### Application Updates

**Docker:**
```bash
docker-compose pull
docker-compose up -d
```

**Kubernetes:**
```bash
kubectl set image deployment/markdn markdn=markdn:v2 -n markdn
kubectl rollout status deployment/markdn -n markdn
```

### Backup Strategy

Content backup:
```bash
#!/bin/bash
# backup.sh
DATE=$(date +%Y%m%d_%H%M%S)
tar -czf markdn-content-$DATE.tar.gz /app/content/
aws s3 cp markdn-content-$DATE.tar.gz s3://backups/markdn/
```

Schedule with cron:
```cron
0 2 * * * /usr/local/bin/backup.sh
```

---

## Support

For deployment issues:
- Check [Troubleshooting](#troubleshooting) section
- Review logs for error messages
- Open an issue: https://github.com/yourusername/markdn/issues
