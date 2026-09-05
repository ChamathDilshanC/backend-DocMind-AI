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
4. Health check path: `/health/live`. (Use the liveness endpoint, not `/health` — `/health` reports every dependency, so a suspended Qdrant or Redis would fail the deploy for an API that is otherwise serving fine.)
5. Add every environment variable listed in `render.yaml` under **Environment**.

Render builds the image from `Dockerfile` on every push to your default branch and redeploys automatically — no separate CI/CD workflow needed for that part (the repo does have `.github/workflows/ci.yml` for build+test-on-PR as a safety net, but deployment itself is Render's job).

## 6. Free-tier caveats — read before you rely on this

- **The Free plan web service sleeps after ~15 minutes of no HTTP traffic**, and cold-starts (roughly a minute) on the next request. Hangfire's job storage is in Postgres (Neon), so queued jobs aren't lost while asleep — they run once the service wakes back up — but nothing processes instantly if the app was idle. Upgrade to a paid instance type to remove this.
- **The container's filesystem is ephemeral.** `Storage__RootPath=/app/storage` (where uploaded PDFs/DOCX live) is wiped on every redeploy and likely on every cold-start/restart cycle too. This is fine for testing the pipeline, **not fine for anyone's documents you actually care about** — the fix is implementing `IStorageService` against a proper object store (Cloudflare R2 and Backblaze B2 both have workable free tiers, S3-compatible) before this goes further than a demo.
- **GitHub Models rate limits** apply if you're using that instead of real OpenAI — expect occasional 429s under any real load.
- **Qdrant Cloud free clusters suspend after a stretch with no requests**, and a suspended cluster is unreachable. The app no longer dies when that happens (`QdrantWarmupService` retries in the background instead of aborting startup), but vector search stays broken until the cluster is back — set up the keep-alive ping below.

## 6a. Keeping the free tier awake

Two layers, and you want both:

1. **External uptime ping (does the real work).** Point a free scheduler at
   `https://<your-service>.onrender.com/health` every **10 minutes**.
   [cron-job.org](https://cron-job.org) or [UptimeRobot](https://uptimerobot.com) both do this on a free
   account. `/health` touches Postgres, Redis *and* Qdrant, so one request keeps the Render service, the
   Neon database and the Qdrant cluster all out of their idle-suspend windows.
2. **In-process keep-alive (backup).** `QdrantWarmupService` pings Qdrant every 10 minutes on its own,
   tunable via `Qdrant__KeepAliveInterval` (a `HH:MM:SS` value; `00:00:00` disables it). On its own this
   is *not* enough — it stops running the moment the Render free instance sleeps, which is precisely when
   Qdrant starts drifting toward suspension. It covers the gaps between external pings and any window
   where the external scheduler is down.

Two things worth knowing before you rely on layer 1: the Render free plan gives you **750 instance-hours
per month across the whole account**, and pinging one service around the clock consumes essentially all
of them; and keeping a free instance permanently awake is not what the free plan is for. If this becomes
something real rather than a demo, the honest fix is Render's paid instance type, not a louder pinger.

## 7. CORS + Google OAuth origin

`Cors__AllowedOrigins__0` should be your real deployed frontend origin (already defaulted to `https://frontend-doc-mind-ai.vercel.app` in the committed `appsettings.json` and `render.yaml`). If your Vercel URL changes, update both this app setting and the Google Cloud Console OAuth Client's Authorized JavaScript origins.

## 8. Post-deploy checklist

- [ ] `https://<your-service>.onrender.com/` shows the status/documentation page, all four checks green
- [ ] `https://<your-service>.onrender.com/health/live` returns `Healthy` (this is what Render polls)
- [ ] `https://<your-service>.onrender.com/health` returns JSON with `"status":"Healthy"` and every one of `postgresql`, `redis`, `qdrant`, `ai-provider` also `Healthy`
- [ ] Register a user, log in, get a JWT
- [ ] Upload a document, confirm it reaches `Completed`
- [ ] Ask a question in chat, confirm a grounded answer with citations comes back
- [ ] `/hangfire` dashboard loads and rejects non-admin users
- [ ] An external 10-minute ping is scheduled against `/health` (see 6a)

## Cost

Every piece of this stack (Render free web service, Neon free Postgres, Upstash free Redis, Qdrant Cloud free cluster, GitHub Models free tier) has a genuinely free tier suitable for a personal project — the tradeoffs are the sleep/cold-start behavior and ephemeral storage above, not money.
