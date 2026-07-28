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

## Production

Use environment variables (ASP.NET Core config automatically maps `Jwt__SigningKey`-style env vars to `Jwt:SigningKey`) or a proper secret manager (Azure Key Vault, AWS Secrets Manager, etc.) — never a committed file.

## Never commit

- `appsettings.Development.json` (gitignored)
- Any `appsettings.*.local.json`
- `.env` files
