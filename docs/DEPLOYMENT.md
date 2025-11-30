# Ghid de Deployment

Acest document descrie procesul de deployment pentru SMU 3.0 pe Azure App Service.

## Cuprins

1. [Cerinte](#cerinte)
2. [Setup Azure](#setup-azure)
3. [Configurare Supabase](#configurare-supabase)
4. [GitHub Actions CI/CD](#github-actions-cicd)
5. [Environment Variables](#environment-variables)
6. [Monitoring](#monitoring)
7. [Troubleshooting](#troubleshooting)

---

## Cerinte

### Azure Resources

- Azure Subscription (Free tier e suficient pentru inceput)
- Azure App Service (Linux, B1 tier minim pentru Blazor Server)
- Azure Application Insights (optional, pentru monitoring)

### External Services

- Supabase account cu proiect PostgreSQL creat
- GitHub repository

### Local Tools

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Setup Azure

### 1. Creaza Resource Group

```bash
# Login la Azure
az login

# Creaza resource group
az group create \
  --name rg-smu \
  --location westeurope
```

### 2. Creaza App Service Plan

```bash
# Creaza plan (B1 tier pentru Blazor Server)
az appservice plan create \
  --name plan-smu \
  --resource-group rg-smu \
  --sku B1 \
  --is-linux
```

### 3. Creaza Web App

```bash
# Creaza web app
az webapp create \
  --resource-group rg-smu \
  --plan plan-smu \
  --name smu3 \
  --runtime "DOTNETCORE:8.0"
```

### 4. Configureaza Settings

```bash
# Seteaza connection string
az webapp config connection-string set \
  --resource-group rg-smu \
  --name smu3 \
  --settings DefaultConnection="Host=xxx.supabase.co;Database=postgres;Username=xxx;Password=xxx" \
  --connection-string-type PostgreSQL

# Seteaza app settings
az webapp config appsettings set \
  --resource-group rg-smu \
  --name smu3 \
  --settings ASPNETCORE_ENVIRONMENT=Production
```

### 5. Enable WebSockets (pentru SignalR)

```bash
az webapp config set \
  --resource-group rg-smu \
  --name smu3 \
  --web-sockets-enabled true
```

---

## Configurare Supabase

### 1. Obtine Connection String

1. Login la [Supabase Dashboard](https://app.supabase.com)
2. Selecteaza proiectul SMU
3. Mergi la **Settings** → **Database**
4. Copiaza **Connection string** (URI format)

### 2. Format Connection String pentru EF Core

```
Host=db.xxxxxxxxxxxx.supabase.co;
Port=5432;
Database=postgres;
Username=postgres;
Password=YOUR_PASSWORD;
SSL Mode=Require;
Trust Server Certificate=true
```

### 3. Aplica Migrarile

```bash
# Local - aplica migrarile la Supabase
dotnet ef database update --connection "Host=db.xxx.supabase.co;..."
```

### 4. Seed Data Initial (optional)

```bash
# Ruleaza seed-ul pentru conturi demo
dotnet run --seed
```

---

## GitHub Actions CI/CD

### 1. Obtine Publish Profile

```bash
# Descarca publish profile
az webapp deployment list-publishing-profiles \
  --resource-group rg-smu \
  --name smu3 \
  --xml > publish-profile.xml
```

### 2. Adauga GitHub Secrets

1. Mergi la repository → **Settings** → **Secrets and variables** → **Actions**
2. Adauga urmatoarele secrets:

| Secret Name | Value |
|-------------|-------|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Continutul din publish-profile.xml |
| `AZURE_WEBAPP_NAME` | smu3 |

### 3. Workflow File

Fisierul `.github/workflows/deploy.yml` este deja configurat.

Workflow-ul:
1. **Build**: Compileaza aplicatia si ruleaza testele
2. **Deploy to Staging** (pe PR): Deploy la slot de staging
3. **Deploy to Production** (pe push main): Deploy la production

### 4. Manual Deployment

```bash
# Deploy manual fara CI/CD
dotnet publish -c Release -o ./publish
az webapp deploy \
  --resource-group rg-smu \
  --name smu3 \
  --src-path ./publish \
  --type zip
```

---

## Environment Variables

### Production Settings

| Variable | Descriere | Exemplu |
|----------|-----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | `Host=...` |
| `Logging__LogLevel__Default` | Log level | `Warning` |
| `AllowedHosts` | Allowed hosts | `*.azurewebsites.net` |

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*.azurewebsites.net;smu3.azurewebsites.net",
  "ApplicationInsights": {
    "ConnectionString": "YOUR_APP_INSIGHTS_CONNECTION_STRING"
  }
}
```

### Setare via Azure Portal

1. App Service → **Configuration** → **Application settings**
2. Adauga fiecare setting ca **New application setting**
3. Pentru connection strings, foloseste **Connection strings** tab

### Setare via Azure CLI

```bash
# Application settings
az webapp config appsettings set \
  --resource-group rg-smu \
  --name smu3 \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    Logging__LogLevel__Default=Warning

# Connection strings
az webapp config connection-string set \
  --resource-group rg-smu \
  --name smu3 \
  --settings DefaultConnection="Host=xxx;..." \
  --connection-string-type PostgreSQL
```

---

## Monitoring

### Application Insights Setup

```bash
# Creaza Application Insights
az monitor app-insights component create \
  --app smu-insights \
  --location westeurope \
  --resource-group rg-smu \
  --application-type web

# Obtine connection string
az monitor app-insights component show \
  --app smu-insights \
  --resource-group rg-smu \
  --query connectionString
```

### Integreaza in Aplicatie

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

### Health Checks

Aplicatia expune endpoint `/health` pentru monitoring:

```bash
# Verifica health
curl https://smu3.azurewebsites.net/health
```

### Logs

```bash
# Activeaza logs
az webapp log config \
  --resource-group rg-smu \
  --name smu3 \
  --docker-container-logging filesystem

# Stream logs live
az webapp log tail \
  --resource-group rg-smu \
  --name smu3
```

### Metrics Importante

- **Response Time**: < 500ms average
- **Error Rate**: < 1%
- **Memory Usage**: < 80%
- **SignalR Connections**: Monitor concurrent connections

---

## Troubleshooting

### Probleme Comune

#### 1. 500 Internal Server Error

**Cauza**: Eroare la startup sau connection string invalid

**Solutie**:
```bash
# Verifica logs
az webapp log tail --resource-group rg-smu --name smu3

# Verifica connection string
az webapp config connection-string list --resource-group rg-smu --name smu3
```

#### 2. SignalR Nu Functioneaza

**Cauza**: WebSockets dezactivate

**Solutie**:
```bash
# Activeaza WebSockets
az webapp config set \
  --resource-group rg-smu \
  --name smu3 \
  --web-sockets-enabled true
```

#### 3. Database Connection Timeout

**Cauza**: SSL sau firewall issues cu Supabase

**Solutie**:
- Verifica `SSL Mode=Require` in connection string
- Verifica ca IP-ul Azure App Service e permis in Supabase

#### 4. Slow Cold Start

**Cauza**: App Service plan prea mic sau app idle

**Solutie**:
```bash
# Upgrade la plan mai mare
az appservice plan update \
  --name plan-smu \
  --resource-group rg-smu \
  --sku P1V2

# Sau activeaza Always On
az webapp config set \
  --resource-group rg-smu \
  --name smu3 \
  --always-on true
```

#### 5. Out of Memory

**Cauza**: Memory leak sau plan prea mic pentru Blazor Server

**Solutie**:
```bash
# Verifica memory usage
az monitor metrics list \
  --resource /subscriptions/.../smu3 \
  --metric "MemoryWorkingSet"

# Upgrade plan
az appservice plan update --name plan-smu --sku P1V2
```

### Debug Mode Temporar

```bash
# Activeaza detailed errors temporar
az webapp config appsettings set \
  --resource-group rg-smu \
  --name smu3 \
  --settings ASPNETCORE_ENVIRONMENT=Development

# IMPORTANT: Dezactiveaza dupa debugging!
az webapp config appsettings set \
  --resource-group rg-smu \
  --name smu3 \
  --settings ASPNETCORE_ENVIRONMENT=Production
```

### Rollback Deployment

```bash
# Lista deployment-uri
az webapp deployment list \
  --resource-group rg-smu \
  --name smu3

# Rollback la deployment anterior
az webapp deployment source sync \
  --resource-group rg-smu \
  --name smu3
```

---

## Checklist Pre-Deployment

- [ ] Connection string configurat corect
- [ ] Migrarile aplicate la baza de date
- [ ] WebSockets activat pentru SignalR
- [ ] Environment setat la Production
- [ ] Logging configurat corespunzator
- [ ] Health checks functional
- [ ] SSL certificate valid
- [ ] GitHub Actions secrets configurate
- [ ] Tested pe staging inainte de production

---

## Resurse Aditionale

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Blazor Server Hosting](https://docs.microsoft.com/aspnet/core/blazor/host-and-deploy/server)
- [SignalR Azure Hosting](https://docs.microsoft.com/aspnet/core/signalr/scale#azure-app-service)
- [Supabase PostgreSQL](https://supabase.com/docs/guides/database)
