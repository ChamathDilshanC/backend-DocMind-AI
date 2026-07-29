# Deploying to Render (100% free stack)

This backend deploys as a Docker container on [Render](https://render.com), backed entirely by free-tier managed services — no Azure/AWS subscription needed.

> An earlier version of this guide targeted Azure App Service. That's been dropped (Azure for Students has region restrictions that got in the way) in favor of this stack, which was verified end-to-end — a real container built from this repo's `Dockerfile`, talking to real Neon/Upstash/Qdrant Cloud/GitHub Models, ran register → login → upload → background processing → RAG chat successfully before this guide was written.

## Architecture

```
Frontend (Vercel)
        │  HTTPS + WSS (SignalR)
        ▼
Render Web Service (Docker)  ──runs──▶  DocumentAssistant.API (incl. Hangfire Server in-process)
        │
        ├──▶ Neon (Postgres)         — app data + Hangfire tables
        ├──▶ Upstash (Redis)         — chat/embedding cache
        ├──▶ Qdrant Cloud            — vector search
        └──▶ GitHub Models (or OpenAI) — chat + embeddings
```

## 1. Neon — PostgreSQL

1. Create a project at https://neon.tech (free tier).
2. Copy the connection string it gives you. Neon hands you a `postgresql://user:pass@host/db?sslmode=require` URI — **convert it to Npgsql key-value format**, since that's what `Npgsql.EntityFrameworkCore.PostgreSQL` expects:

   ```
   postgresql://neondb_owner:PASSWORD@ep-xxxx.aws.neon.tech/neondb?sslmode=require
   ```
   becomes
   ```
   Host=ep-xxxx.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=PASSWORD;Ssl Mode=Require;Trust Server Certificate=true
   ```

This is the value for `ConnectionStrings__DefaultConnection`.

## 2. Upstash — Redis

1. Create a free Redis database at https://upstash.com.
2. Use its **TCP/TLS endpoint** directly in `StackExchange.Redis` format — Upstash gives you this ready-made:
   ```
   <endpoint>:6379,password=<password>,ssl=True,abortConnect=False
   ```
   This is the value for `Redis__ConnectionString`.

## 3. Qdrant Cloud — vector database

1. Create a free cluster at https://cloud.qdrant.io.
2. From the cluster's API Keys tab, create a key.
3. You need three values from this:
   - `Qdrant__Host` = the cluster hostname **without** the `https://` prefix (e.g. `xxxxx.us-east-2-0.aws.cloud.qdrant.io`)
   - `Qdrant__GrpcPort` = `6334`
   - `Qdrant__UseHttps` = `true`
   - `Qdrant__ApiKey` = the API key

## 4. AI — GitHub Models (free) or real OpenAI

See `appsettings.README.md` → "Testing for free with GitHub Models" for the free option (rate-limited, fine for personal use), or use a real OpenAI key for production traffic. Either way you're setting `OpenAI__ApiKey` (+ `OpenAI__Endpoint` only for GitHub Models).

## 5. Deploy to Render

**Option A — Blueprint (recommended, one pass through all settings)**

1. Render dashboard → **New** → **Blueprint** → connect this GitHub repo (`backend-DocMind-AI`) → Render reads `render.yaml` from the repo root and creates the service.
2. It'll prompt you for every value marked `sync: false` in `render.yaml` (all the secrets above) — fill them in there. Nothing secret ever needs to touch a committed file.

**Option B — Manual web service**

1. Render dashboard → **New** → **Web Service** → connect the repo.
2. Runtime: **Docker**. Dockerfile path: `./Dockerfile`. Root directory: repo root.
3. Instance type: **Free** is enough to test; see the caveats below before treating this as more than a demo.
4. Health check path: `/health`.
5. Add every environment variable listed in `render.yaml` under **Environment**.

Render builds the image from `Dockerfile` on every push to your default branch and redeploys automatically — no separate CI/CD workflow needed for that part (the repo does have `.github/workflows/ci.yml` for build+test-on-PR as a safety net, but deployment itself is Render's job).

## 6. Free-tier caveats — read before you rely on this

- **The Free plan web service sleeps after ~15 minutes of no HTTP traffic**, and cold-starts (roughly a minute) on the next request. Hangfire's job storage is in Postgres (Neon), so queued jobs aren't lost while asleep — they run once the service wakes back up — but nothing processes instantly if the app was idle. Upgrade to a paid instance type to remove this.
- **The container's filesystem is ephemeral.** `Storage__RootPath=/app/storage` (where uploaded PDFs/DOCX live) is wiped on every redeploy and likely on every cold-start/restart cycle too. This is fine for testing the pipeline, **not fine for anyone's documents you actually care about** — the fix is implementing `IStorageService` against a proper object store (Cloudflare R2 and Backblaze B2 both have workable free tiers, S3-compatible) before this goes further than a demo.
- **GitHub Models rate limits** apply if you're using that instead of real OpenAI — expect occasional 429s under any real load.

## 7. CORS + Google OAuth origin

`Cors__AllowedOrigins__0` should be your real deployed frontend origin (already defaulted to `https://frontend-doc-mind-ai.vercel.app` in the committed `appsettings.json` and `render.yaml`). If your Vercel URL changes, update both this app setting and the Google Cloud Console OAuth Client's Authorized JavaScript origins.

## 8. Post-deploy checklist

- [ ] `https://<your-service>.onrender.com/health` returns `Healthy`
- [ ] Register a user, log in, get a JWT
- [ ] Upload a document, confirm it reaches `Completed`
- [ ] Ask a question in chat, confirm a grounded answer with citations comes back
- [ ] `/hangfire` dashboard loads and rejects non-admin users

## Cost

Every piece of this stack (Render free web service, Neon free Postgres, Upstash free Redis, Qdrant Cloud free cluster, GitHub Models free tier) has a genuinely free tier suitable for a personal project — the tradeoffs are the sleep/cold-start behavior and ephemeral storage above, not money.
