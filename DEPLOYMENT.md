# Deploying to Azure

This guide walks through deploying `backend-DocMind AI` to Azure App Service, with managed PostgreSQL and Redis, plus a vector database option. No Docker/Dockerfile is needed — App Service for Linux runs .NET 8 code directly.

## Architecture on Azure

```
Frontend (Vercel / Azure Static Web Apps / App Service)
        │  HTTPS + WSS (SignalR)
        ▼
Azure App Service (Linux, .NET 8)  ──runs──▶  DocumentAssistant.API (incl. Hangfire Server in-process)
        │
        ├──▶ Azure Database for PostgreSQL – Flexible Server   (app data + Hangfire tables)
        ├──▶ Azure Cache for Redis                              (chat/embedding cache)
        ├──▶ Qdrant Cloud (or Azure Container Apps)             (vector search)
        └──▶ OpenAI API (external)
```

## Prerequisites

- An Azure subscription and the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed, logged in (`az login`).
- This repo pushed to GitHub (already done — `ChamathDilshanC/backend-DocMind-AI`).
- Your OpenAI API key and Google OAuth Client ID ready (see `appsettings.README.md` if you haven't created these yet).

Pick names/region once and reuse them everywhere below:

```bash
RG=docmind-ai-rg
LOCATION=eastus
```

## 1. Resource group

```bash
az group create --name $RG --location $LOCATION
```

## 2. PostgreSQL (Azure Database for PostgreSQL – Flexible Server)

```bash
az postgres flexible-server create \
  --resource-group $RG \
  --name docmind-ai-pg \
  --location $LOCATION \
  --admin-user docmind \
  --admin-password "<CHOOSE-A-STRONG-PASSWORD>" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16

az postgres flexible-server db create \
  --resource-group $RG --server-name docmind-ai-pg --database-name docmind

# Allow Azure services (incl. your App Service) to reach it.
# 0.0.0.0-0.0.0.0 is Azure's special "allow Azure services" rule, not "allow the whole internet".
az postgres flexible-server firewall-rule create \
  --resource-group $RG --name docmind-ai-pg \
  --rule-name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

Connection string (SSL is required by default on Azure Postgres):

```
Host=docmind-ai-pg.postgres.database.azure.com;Port=5432;Database=docmind;Username=docmind;Password=<password>;Ssl Mode=Require
```

## 3. Redis (Azure Cache for Redis)

```bash
az redis create --resource-group $RG --name docmind-ai-redis --location $LOCATION --sku Basic --vm-size c0
az redis list-keys --resource-group $RG --name docmind-ai-redis
```

Connection string for `StackExchange.Redis` (note port **6380** + `ssl=True` — Azure Redis is TLS-only by default):

```
docmind-ai-redis.redis.cache.windows.net:6380,password=<primary-key>,ssl=True,abortConnect=False
```

## 4. Vector database — Qdrant

Azure has no first-party managed Qdrant. Two options:

**Option A — Qdrant Cloud (recommended, simplest)**
1. Sign up at https://cloud.qdrant.io — the free tier is enough for a personal project.
2. Create a cluster, copy its **endpoint host** and **API key**.
3. Use these in the App Settings below: `Qdrant__Host` = the cluster host, `Qdrant__GrpcPort` = `6334`, `Qdrant__UseHttps` = `true`, `Qdrant__ApiKey` = the API key. (This backend already supports an `ApiKey` — added specifically so Qdrant Cloud works.)

**Option B — self-host on Azure Container Apps** (keeps everything inside Azure, more setup):
```bash
az containerapp env create --name docmind-ai-env --resource-group $RG --location $LOCATION
az containerapp create \
  --name docmind-ai-qdrant --resource-group $RG --environment docmind-ai-env \
  --image qdrant/qdrant:latest --target-port 6334 --ingress internal \
  --min-replicas 1 --max-replicas 1
```
Mount a persistent Azure Files share to `/qdrant/storage` if you go this route (Container Apps' local disk is ephemeral by default) — see [Container Apps storage mounts](https://learn.microsoft.com/azure/container-apps/storage-mounts). Leave `Qdrant__ApiKey` empty (self-hosted has no auth configured unless you add one).

## 5. App Service (the API itself)

```bash
az appservice plan create --resource-group $RG --name docmind-ai-plan --location $LOCATION --sku B1 --is-linux
az webapp create --resource-group $RG --plan docmind-ai-plan --name docmind-ai-api --runtime "DOTNETCORE:8.0"
```

`docmind-ai-api` must be globally unique — change it if taken. Your API's URL will be `https://docmind-ai-api.azurewebsites.net`.

**B1 tier, not the Free tier** — the Free tier can't run "Always On", and without it App Service idles the process after ~20 minutes of no HTTP traffic, which kills the in-process Hangfire Server (so background document processing would silently stop working between requests).

```bash
# Always On: keeps the process (and Hangfire Server) alive
az webapp config set --resource-group $RG --name docmind-ai-api --always-on true

# WebSockets: required for SignalR (chat streaming, document progress)
az webapp config set --resource-group $RG --name docmind-ai-api --web-sockets-enabled true
```

## 6. Application Settings (environment variables)

ASP.NET Core config maps App Settings named with `__` (double underscore) to nested `appsettings.json` keys, e.g. `Jwt__SigningKey` → `Jwt:SigningKey`. Set every value that's blank in the committed `appsettings.json`:

```bash
az webapp config appsettings set --resource-group $RG --name docmind-ai-api --settings \
  ASPNETCORE_ENVIRONMENT="Production" \
  ConnectionStrings__DefaultConnection="Host=docmind-ai-pg.postgres.database.azure.com;Port=5432;Database=docmind;Username=docmind;Password=<password>;Ssl Mode=Require" \
  Jwt__Issuer="DocMindAI" \
  Jwt__Audience="DocMindAI" \
  Jwt__SigningKey="<random 32+ byte base64 secret - generate a NEW one for production, don't reuse your local dev one>" \
  OpenAI__ApiKey="sk-..." \
  OpenAI__ChatModel="gpt-4o" \
  OpenAI__EmbeddingModel="text-embedding-3-small" \
  Authentication__Google__ClientId="<your-client-id>.apps.googleusercontent.com" \
  Qdrant__Host="<your-qdrant-cloud-host>" \
  Qdrant__GrpcPort="6334" \
  Qdrant__UseHttps="true" \
  Qdrant__ApiKey="<your-qdrant-cloud-api-key>" \
  Redis__ConnectionString="docmind-ai-redis.redis.cache.windows.net:6380,password=<redis-key>,ssl=True,abortConnect=False" \
  Storage__RootPath="/home/storage" \
  Cors__AllowedOrigins__0="https://frontend-doc-mind-ai.vercel.app"
```

Notes:
- **`Storage__RootPath=/home/storage`**: on Linux App Service, only the `/home` path is persisted across restarts and redeploys (it's backed by an Azure Files share); anywhere else on disk is ephemeral. Uploaded documents must live under `/home` or they'll vanish on the next restart/scale event. This works for a single always-on instance; it is **not** safe if you later scale to multiple instances (`/home` is shared, but concurrent local-disk-style access patterns aren't — migrate to `IStorageService` backed by Azure Blob Storage before scaling out).
- **`Jwt__SigningKey`**: generate a fresh one, don't reuse the value from your local `dotnet user-secrets`. PowerShell: `[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))`.
- **Google OAuth**: add your production frontend origin (e.g. `https://docmind.yourdomain.com`) to the OAuth Client's Authorized JavaScript origins in Google Cloud Console — `http://localhost:3000` alone won't work once deployed.
- **Secrets via Key Vault instead of plain App Settings (optional, more secure)**: create secrets in an Azure Key Vault, grant the App Service's managed identity `get` access, then set the App Setting *value* to `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<name>/)` — App Service resolves it at runtime. No code change needed for this.

## 7. Deploy the code

Simplest path: GitHub Actions, deploying on every push to `main`. Get a publish profile and store it as a GitHub secret:

```bash
az webapp deployment list-publishing-profiles --resource-group $RG --name docmind-ai-api --xml > publishprofile.xml
```

In GitHub → this repo → Settings → Secrets and variables → Actions → New repository secret: name it `AZURE_WEBAPP_PUBLISH_PROFILE`, paste the contents of `publishprofile.xml`. **Delete `publishprofile.xml` locally afterward** — it's a credential, never commit it.

The workflow file is at `.github/workflows/azure-deploy.yml` (added alongside this guide). Push to `main` and it builds + deploys automatically. Or deploy once manually without CI:

```bash
dotnet publish src/DocumentAssistant.API -c Release -o ./publish
cd publish && zip -r ../publish.zip . && cd ..
az webapp deploy --resource-group $RG --name docmind-ai-api --src-path publish.zip --type zip
```

## 8. First boot

`RunMigrationsOnStartup` defaults to `true`, so the app applies EF Core migrations automatically on first request after deploy — no manual migration step needed for a single-instance deployment like this one. Watch it happen:

```bash
az webapp log tail --resource-group $RG --name docmind-ai-api
```

Then verify:

```bash
curl https://docmind-ai-api.azurewebsites.net/health
# -> Healthy
```

If it's not healthy, check the log stream above — almost always a connection string or firewall issue with Postgres/Redis.

## 9. Point the frontend at it

In `frontend-DocMind AI/.env.local` (or wherever you deploy the frontend's env config):

```
NEXT_PUBLIC_API_BASE_URL=https://docmind-ai-api.azurewebsites.net
```

And make sure `Cors__AllowedOrigins__0` on the App Service (step 6) matches the frontend's real deployed origin exactly (scheme + host, no trailing slash), or the browser will block every request with a CORS error.

## 10. Post-deploy checklist

- [ ] `/health` returns `Healthy`
- [ ] `/swagger` — disabled in Production by default (see `Program.cs`: `if (app.Environment.IsDevelopment())`). Temporarily set `ASPNETCORE_ENVIRONMENT=Development` to poke it if needed, then set it back to `Production` — don't leave Swagger exposed permanently on a public production URL.
- [ ] Register a user, log in, confirm you get a JWT
- [ ] Upload a document, confirm it reaches `Completed` (this is the step that needs a real, working `OpenAI__ApiKey`)
- [ ] Ask a question in a chat, confirm streaming works (needs WebSockets enabled — step 5)
- [ ] `/hangfire` dashboard loads and is rejected for non-admin users (it's gated to the `Admin` role)

## Rough monthly cost (as of writing, East US, lowest viable tiers)

| Resource | Tier | ~Cost/mo |
|---|---|---|
| App Service Plan | B1 (Always On required) | ~$13 |
| PostgreSQL Flexible Server | Burstable B1ms, 32GB | ~$15 |
| Azure Cache for Redis | Basic C0 (250MB) | ~$16 |
| Qdrant Cloud | Free tier | $0 |
| OpenAI API | Pay-per-token | usage-based |

Prices change and vary by region — check the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) before committing.
