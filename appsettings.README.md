# Configuration & Secrets

`appsettings.json` (committed) only ever contains placeholder/non-secret defaults. Real values live in one of these places, none of which are committed:

## Local development — `dotnet user-secrets`

Run these from `src/DocumentAssistant.API/`:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=docmind;Username=docmind;Password=<your-postgres-password>"
dotnet user-secrets set "Jwt:SigningKey" "<random 32+ byte base64 secret>"
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
dotnet user-secrets set "Authentication:Google:ClientId" "<your-client-id>.apps.googleusercontent.com"
```

### Where to get each value

| Secret | Where to get it |
|---|---|
| `OpenAI:ApiKey` | https://platform.openai.com/api-keys — create a new secret key |
| `Authentication:Google:ClientId` | Google Cloud Console → APIs & Services → Credentials → Create Credentials → OAuth Client ID → Application type "Web application" → Authorized JavaScript origin `http://localhost:3000`. Only the **Client ID** is needed (no client secret) — the backend verifies a Google-issued ID token, it does not perform an authorization-code exchange. |
| `Jwt:SigningKey` | Generate locally, e.g. in PowerShell: `[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))` |
| `ConnectionStrings:DefaultConnection` password | Whatever you set in the root repo's `.env` for the `postgres` docker-compose service — must match. |

> The root `docker-compose.yml` maps Postgres to host port **5433** (not the default 5432) to avoid clashing with a locally-installed PostgreSQL service, if one happens to already be running on your machine.

## Testing for free with GitHub Models (no OpenAI billing needed)

For local development/testing only — not for production — you can point the OpenAI connector at [GitHub Models](https://github.com/marketplace/models) instead of paying OpenAI directly. GitHub Models exposes an OpenAI-compatible endpoint, authenticated with a GitHub **personal access token** (no scopes/permissions need to be checked when creating it).

```
dotnet user-secrets set "OpenAI:Endpoint" "https://models.inference.ai.azure.com"
dotnet user-secrets set "OpenAI:ApiKey" "<your GitHub personal access token>"
```

`OpenAI:ChatModel` (`gpt-4o`) and `OpenAI:EmbeddingModel` (`text-embedding-3-small`) both work unchanged against GitHub Models — same model names, different endpoint.

**Never paste a token directly into a chat, issue, PR, or any file that might get committed.** Always put it straight into `dotnet user-secrets` (or an environment variable) and nowhere else. If a token is ever pasted somewhere it shouldn't be, treat it as compromised immediately — revoke it at GitHub → Settings → Developer settings → Personal access tokens, and generate a new one.

GitHub Models' free tier is rate-limited (fine for solo dev/testing, not for real traffic) — leave `OpenAI:Endpoint` unset to fall back to the real OpenAI API for anything production-like.

## Production

Use environment variables (ASP.NET Core config automatically maps `Jwt__SigningKey`-style env vars to `Jwt:SigningKey`) or your host's secret manager — never a committed file.

For the full deployment walkthrough (Render + Neon + Upstash + Qdrant Cloud, exact environment variables, the Docker build), see **[DEPLOYMENT.md](DEPLOYMENT.md)**.

## Never commit

- `appsettings.Development.json` (gitignored)
- Any `appsettings.*.local.json`
- `.env` files
