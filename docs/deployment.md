# Deployment & Infrastructure

## Overview

Low Rollers is deployed on Azure Container Apps using .NET Aspire for orchestration. The infrastructure is designed for cost-efficiency with scheduled scaling for monthly game sessions.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Azure Container Apps                           │
│                          (Environment: lowrollers)                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐       │
│  │  lowrollers-web  │  │  lowrollers-api  │  │  livekit-server  │       │
│  │  (Angular SPA)   │  │  (.NET 10 API)   │  │  (Video SFU)     │       │
│  │                  │  │  + SignalR       │  │                  │       │
│  │  Replicas: 1     │  │  Replicas: 1-3   │  │  Replicas: 1     │       │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘       │
│           │                     │                     │                  │
│           └─────────────────────┼─────────────────────┘                  │
│                                 │                                        │
└─────────────────────────────────┼────────────────────────────────────────┘
                                  │
                    ┌─────────────┼─────────────┐
                    │             │             │
              ┌─────▼─────┐ ┌─────▼─────┐ ┌─────▼─────┐
              │ PostgreSQL│ │   Redis   │ │  SignalR  │
              │ Flexible  │ │ (Cache)   │ │ Service   │
              │ Server    │ │           │ │ (Managed) │
              └───────────┘ └───────────┘ └───────────┘
```

---

## Azure Resources

### Container Apps Environment
| Resource | SKU/Tier | Purpose |
|----------|----------|---------|
| Container Apps Environment | Consumption | Serverless containers |
| lowrollers-web | 0.25 vCPU, 0.5 GB | Angular frontend |
| lowrollers-api | 0.5 vCPU, 1 GB | .NET API + SignalR |
| livekit-server | 1 vCPU, 2 GB | Video SFU |

### Data Services
| Resource | SKU/Tier | Purpose |
|----------|----------|---------|
| PostgreSQL Flexible Server | Burstable B1ms | Primary database |
| Azure Cache for Redis | Basic C0 | Session/cache |
| Azure SignalR Service | Free (20 connections) | WebSocket scaling |

### Networking
| Resource | Purpose |
|----------|---------|
| Virtual Network | Container Apps integration |
| Private Endpoints | Secure database access |
| Application Gateway | (Optional) Custom domain, SSL |

---

## Cost Optimization

### Scheduled Scaling

Since games are monthly (~4 hours), we optimize for cost:

```yaml
# Scale to zero between games
Schedule:
  - Cron: "0 18 * * 5"  # Scale up Friday 6 PM
    MinReplicas: 1
    MaxReplicas: 3
  - Cron: "0 2 * * 6"   # Scale down Saturday 2 AM
    MinReplicas: 0
    MaxReplicas: 0
```

### Azure Automation Runbooks

```powershell
# Start-LowRollers.ps1
$env = Get-AzContainerAppManagedEnv -Name "lowrollers" -ResourceGroupName "rg-lowrollers"
Update-AzContainerApp -Name "lowrollers-api" -MinReplicas 1
Update-AzContainerApp -Name "lowrollers-web" -MinReplicas 1
Start-AzPostgreSqlFlexibleServer -Name "psql-lowrollers" -ResourceGroupName "rg-lowrollers"

# Stop-LowRollers.ps1
Update-AzContainerApp -Name "lowrollers-api" -MinReplicas 0
Update-AzContainerApp -Name "lowrollers-web" -MinReplicas 0
Stop-AzPostgreSqlFlexibleServer -Name "psql-lowrollers" -ResourceGroupName "rg-lowrollers"
```

### Estimated Monthly Cost

| Service | Active Hours | Cost |
|---------|--------------|------|
| Container Apps (scale-to-zero) | ~8 hrs | $5-10 |
| PostgreSQL (stopped most of time) | ~8 hrs | $10-15 |
| Redis Basic | Always on | $15 |
| SignalR Free Tier | Always on | $0 |
| Storage (logs, etc.) | - | $1-2 |
| **Total** | | **$31-42/month** |

---

## Infrastructure as Code

### Bicep Template Structure

```
deploy/
├── main.bicep                 # Main deployment
├── modules/
│   ├── container-apps.bicep   # Container Apps environment
│   ├── postgresql.bicep       # PostgreSQL Flexible Server
│   ├── redis.bicep            # Azure Cache for Redis
│   ├── signalr.bicep          # SignalR Service
│   └── networking.bicep       # VNet, private endpoints
├── parameters/
│   ├── dev.parameters.json    # Development
│   └── prod.parameters.json   # Production
└── scripts/
    ├── deploy.sh              # Deployment script
    ├── start-services.ps1     # Start all services
    └── stop-services.ps1      # Stop all services
```

### Key Bicep Resources

```bicep
// Container App
resource apiApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'lowrollers-api'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      secrets: [
        { name: 'db-connection', value: postgresConnectionString }
        { name: 'redis-connection', value: redisConnectionString }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${containerRegistry}/lowrollers-api:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ConnectionStrings__Database', secretRef: 'db-connection' }
            { name: 'ConnectionStrings__Redis', secretRef: 'redis-connection' }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http-rule'
            http: { metadata: { concurrentRequests: '100' } }
          }
        ]
      }
    }
  }
}
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  AZURE_CONTAINER_REGISTRY: crlowrollers
  RESOURCE_GROUP: rg-lowrollers

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Build API
        run: dotnet build src/LowRollers.Api

      - name: Test API
        run: dotnet test tests/LowRollers.Api.Tests

      - name: Build Web
        run: |
          cd src/LowRollers.Web
          npm ci
          npm run build

      - name: Test Web
        run: |
          cd src/LowRollers.Web
          npm test -- --watch=false

  deploy:
    needs: build-and-test
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Build and push API image
        run: |
          az acr build \
            --registry ${{ env.AZURE_CONTAINER_REGISTRY }} \
            --image lowrollers-api:${{ github.sha }} \
            --file src/LowRollers.Api/Dockerfile \
            src/

      - name: Build and push Web image
        run: |
          az acr build \
            --registry ${{ env.AZURE_CONTAINER_REGISTRY }} \
            --image lowrollers-web:${{ github.sha }} \
            --file src/LowRollers.Web/Dockerfile \
            src/

      - name: Deploy to Container Apps
        run: |
          az containerapp update \
            --name lowrollers-api \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lowrollers-api:${{ github.sha }}

          az containerapp update \
            --name lowrollers-web \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lowrollers-web:${{ github.sha }}
```

---

## Environment Configuration

### Application Settings

```json
{
  "ConnectionStrings": {
    "Database": "Host=psql-lowrollers.postgres.database.azure.com;Database=lowrollers;...",
    "Redis": "redis-lowrollers.redis.cache.windows.net:6380,password=...,ssl=True"
  },
  "Azure": {
    "SignalR": {
      "ConnectionString": "Endpoint=https://signalr-lowrollers.service.signalr.net;..."
    }
  },
  "LiveKit": {
    "Host": "wss://livekit-lowrollers.azurecontainerapps.io",
    "ApiKey": "...",
    "ApiSecret": "..."
  },
  "Jwt": {
    "Secret": "...",
    "Issuer": "lowrollers",
    "Audience": "lowrollers-users"
  }
}
```

### Secrets Management

| Secret | Storage |
|--------|---------|
| Database connection string | Azure Key Vault |
| Redis connection string | Azure Key Vault |
| SignalR connection string | Azure Key Vault |
| LiveKit API secret | Azure Key Vault |
| JWT signing key | Azure Key Vault |

---

## Monitoring & Logging

### Application Insights

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom metrics
telemetryClient.TrackMetric("HandsPlayed", handsCount);
telemetryClient.TrackMetric("ActiveTables", tablesCount);
telemetryClient.TrackMetric("ActionLatency", latencyMs);
```

### Alerts

| Metric | Threshold | Action |
|--------|-----------|--------|
| Response time P95 | > 500ms | Email notification |
| Error rate | > 1% | Email notification |
| CPU usage | > 80% for 5 min | Scale out |
| Failed health checks | > 2 consecutive | Page on-call |

### Log Analytics Queries

```kusto
// Action latency distribution
customMetrics
| where name == "ActionLatency"
| summarize percentiles(value, 50, 95, 99) by bin(timestamp, 1h)

// Errors by endpoint
requests
| where success == false
| summarize count() by name, resultCode
| order by count_ desc
```

---

## Disaster Recovery

### Backup Strategy

| Resource | Frequency | Retention |
|----------|-----------|-----------|
| PostgreSQL | Every 6 hours | 7 days |
| Container images | On deploy | Last 10 versions |
| Configuration | Git | Forever |

### Recovery Procedures

1. **Database restore**: Point-in-time restore from Azure backup
2. **Application rollback**: Deploy previous container image tag
3. **Configuration restore**: Redeploy from Git

---

## Security

### Network Security

- VNet integration for Container Apps
- Private endpoints for PostgreSQL and Redis
- No public database access
- TLS 1.3 everywhere

### Authentication

- Guest sessions via JWT
- Short-lived tokens (24 hours)
- Session storage in Redis

### Compliance

- No PII stored (display names only)
- No real money (play chips)
- HTTPS everywhere
- Regular dependency updates

---

## Local Development

### .NET Aspire

```bash
# Start all services locally
cd src/LowRollers.AppHost
dotnet run

# Opens Aspire dashboard at https://localhost:17224
# - PostgreSQL on localhost:5432
# - Redis on localhost:6379
# - API on https://localhost:7001
# - Web on https://localhost:4200
```

### Docker Compose (Alternative)

```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: lowrollers
      POSTGRES_PASSWORD: devpassword
    ports:
      - "5432:5432"

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  livekit:
    image: livekit/livekit-server:latest
    command: --dev
    ports:
      - "7880:7880"
      - "7881:7881"
```

---

## Implementation Tasks

- [ ] **Create Bicep infrastructure templates** (Task ID: `deploy-01`)
- [ ] **Set up Azure Container Registry** (Task ID: `deploy-02`)
- [ ] **Configure GitHub Actions CI/CD** (Task ID: `deploy-03`)
- [ ] **Set up Application Insights monitoring** (Task ID: `deploy-04`)
- [ ] **Configure Azure Automation for scheduled start/stop** (Task ID: `deploy-05`)
- [ ] **Set up Azure Key Vault for secrets** (Task ID: `deploy-06`)
- [ ] **Configure custom domain and SSL** (Task ID: `deploy-07`)
- [ ] **Create backup and recovery runbooks** (Task ID: `deploy-08`)

---

*See also: [architecture.md](./architecture.md) | [tech-stack.md](./tech-stack.md)*
