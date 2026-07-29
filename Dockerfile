# syntax=docker/dockerfile:1

# --- Build stage -------------------------------------------------------
# global.json pins SDK 9.0.306 (net8.0 target) — use the 9.0 SDK image here, not 8.0.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached across builds
# unless a .csproj actually changes.
COPY DocumentAssistant.sln Directory.Build.props global.json ./
COPY src/DocumentAssistant.Domain/DocumentAssistant.Domain.csproj src/DocumentAssistant.Domain/
COPY src/DocumentAssistant.Shared/DocumentAssistant.Shared.csproj src/DocumentAssistant.Shared/
COPY src/DocumentAssistant.Application/DocumentAssistant.Application.csproj src/DocumentAssistant.Application/
COPY src/DocumentAssistant.Persistence/DocumentAssistant.Persistence.csproj src/DocumentAssistant.Persistence/
COPY src/DocumentAssistant.Infrastructure/DocumentAssistant.Infrastructure.csproj src/DocumentAssistant.Infrastructure/
COPY src/DocumentAssistant.SemanticKernel/DocumentAssistant.SemanticKernel.csproj src/DocumentAssistant.SemanticKernel/
COPY src/DocumentAssistant.VectorStore/DocumentAssistant.VectorStore.csproj src/DocumentAssistant.VectorStore/
COPY src/DocumentAssistant.API/DocumentAssistant.API.csproj src/DocumentAssistant.API/
COPY src/DocumentAssistant.Tests/DocumentAssistant.Tests.csproj src/DocumentAssistant.Tests/

RUN dotnet restore DocumentAssistant.sln

COPY . .
RUN dotnet publish src/DocumentAssistant.API/DocumentAssistant.API.csproj \
    -c Release -o /app/publish --no-restore

# --- Runtime stage -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as a non-root user.
RUN groupadd --system --gid 1000 appgroup \
    && useradd --system --uid 1000 --gid appgroup appuser

COPY --from=build /app/publish .

# Storage:RootPath and the Serilog file sink both need a writable directory.
# On Render's free tier this is ephemeral (wiped on every redeploy/restart) —
# see DEPLOYMENT.md for why documents therefore need Blob-style storage for
# anything beyond quick testing.
RUN mkdir -p /app/storage /app/logs && chown -R appuser:appgroup /app

USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Render assigns a dynamic port via $PORT at container start and routes traffic to it.
# Default 8080 covers `docker run` locally, where $PORT isn't set.
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "export ASPNETCORE_HTTP_PORTS=${PORT:-8080} && exec dotnet DocumentAssistant.API.dll"]
