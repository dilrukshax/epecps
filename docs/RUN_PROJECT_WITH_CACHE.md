# Run Project With Cached Dev Script

This guide explains how to run EPECPS with local caching so you avoid downloading dependencies/images every time.

## Why this script

- Reuses local npm cache (`.cache/dev/npm`)
- Reuses local NuGet cache (`.cache/dev/nuget`)
- Uses Docker build cache and `pull=missing` by default
- Supports offline-style startup (`pull=never`)

## Script Location

- [dev-cache.sh](/Users/dilandilaruksha/Project/epecps/scripts/dev-cache.sh)

## Quick Start

From project root:

```bash
# 1) Warm dependency caches (npm + NuGet)
./scripts/dev-cache.sh prepare

# 2) Start docker stack using cache (only pull missing images)
./scripts/dev-cache.sh up

# 3) Check status
./scripts/dev-cache.sh ps
```

## Useful Commands

```bash
# Start with no internet image pull
./scripts/dev-cache.sh up-offline

# Rebuild only changed service
./scripts/dev-cache.sh rebuild backend
./scripts/dev-cache.sh rebuild frontend

# Rebuild all services
./scripts/dev-cache.sh rebuild

# Logs
./scripts/dev-cache.sh logs
./scripts/dev-cache.sh logs backend

# Stop stack
./scripts/dev-cache.sh down

# Clear local cache
./scripts/dev-cache.sh clean-cache
```

## Notes

- `up` uses Docker pull policy `missing` by default.
- `up-offline` uses Docker pull policy `never`.
- If your Docker Compose version does not support `--pull` on `up`, the script falls back to cached build/start.
- If `dotnet` is not installed, backend cache warm-up is skipped gracefully.

## Full Script Code

```bash
#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

CACHE_ROOT="${CACHE_ROOT:-$ROOT_DIR/.cache/dev}"
NPM_CACHE_DIR="${NPM_CACHE_DIR:-$CACHE_ROOT/npm}"
NUGET_CACHE_DIR="${NUGET_CACHE_DIR:-$CACHE_ROOT/nuget}"

log() {
  printf '[dev-cache] %s\n' "$1"
}

warn() {
  printf '[dev-cache][warn] %s\n' "$1"
}

usage() {
  cat <<'EOF'
Usage: ./scripts/dev-cache.sh <command> [args]

Commands:
  prepare             Warm local dependency caches (npm + NuGet)
  up                  Start Docker stack using cache (pull policy: missing)
  up-offline          Start Docker stack without pulling images from internet
  rebuild [service]   Rebuild all or one service with cache, then start
  down                Stop Docker stack
  ps                  Show Docker stack status
  logs [service]      Tail logs for all services or one service
  clean-cache         Remove local dev cache directory (.cache/dev)

Environment Variables:
  CACHE_ROOT          Base cache directory (default: .cache/dev)
  NPM_CACHE_DIR       npm cache path (default: $CACHE_ROOT/npm)
  NUGET_CACHE_DIR     NuGet cache path (default: $CACHE_ROOT/nuget)
  PULL_POLICY         Docker pull policy for "up" (default: missing)
EOF
}

ensure_dir() {
  mkdir -p "$1"
}

ensure_env_file() {
  if [[ -f .env ]]; then
    return
  fi

  if [[ -f .env.docker.example ]]; then
    cp .env.docker.example .env
    log "Created .env from .env.docker.example"
    return
  fi

  warn "Missing .env and .env.docker.example. Docker commands may fail."
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    warn "Required command not found: $1"
    return 1
  fi
  return 0
}

prepare_frontend_cache() {
  if ! require_command npm; then
    return
  fi

  ensure_dir "$NPM_CACHE_DIR"
  log "Preparing frontend cache at: $NPM_CACHE_DIR"
  (
    cd "$ROOT_DIR/frontend/epecps-web"
    npm ci --cache "$NPM_CACHE_DIR" --prefer-offline --no-audit --no-fund
  )
}

prepare_backend_cache() {
  if ! require_command dotnet; then
    warn "dotnet not found. Skipping backend cache warm-up."
    return
  fi

  ensure_dir "$NUGET_CACHE_DIR"
  log "Preparing backend cache at: $NUGET_CACHE_DIR"
  dotnet restore "$ROOT_DIR/epecps.sln" --packages "$NUGET_CACHE_DIR" --nologo
}

prepare_all() {
  ensure_dir "$CACHE_ROOT"
  prepare_frontend_cache
  prepare_backend_cache
  log "Cache warm-up complete."
}

compose_supports_pull_flag() {
  docker compose up --help 2>/dev/null | grep -q -- '--pull'
}

compose_up_with_policy() {
  local policy="$1"
  ensure_env_file

  if compose_supports_pull_flag; then
    log "Starting Docker stack with pull policy: $policy"
    docker compose up -d --build --pull "$policy"
  else
    warn "This Docker Compose version does not support '--pull' on up. Using cached build without explicit pull policy."
    docker compose up -d --build
  fi
}

compose_rebuild() {
  local service="${1:-}"
  ensure_env_file

  if [[ -n "$service" ]]; then
    log "Rebuilding service: $service"
    docker compose build "$service"
    docker compose up -d "$service"
  else
    log "Rebuilding all services"
    docker compose build
    docker compose up -d
  fi
}

main() {
  local cmd="${1:-help}"
  local arg="${2:-}"

  case "$cmd" in
    prepare)
      prepare_all
      ;;
    up)
      require_command docker || exit 1
      compose_up_with_policy "${PULL_POLICY:-missing}"
      ;;
    up-offline)
      require_command docker || exit 1
      compose_up_with_policy "never"
      ;;
    rebuild)
      require_command docker || exit 1
      compose_rebuild "$arg"
      ;;
    down)
      require_command docker || exit 1
      docker compose down
      ;;
    ps)
      require_command docker || exit 1
      docker compose ps
      ;;
    logs)
      require_command docker || exit 1
      if [[ -n "$arg" ]]; then
        docker compose logs -f "$arg"
      else
        docker compose logs -f
      fi
      ;;
    clean-cache)
      if [[ -d "$CACHE_ROOT" ]]; then
        rm -rf "$CACHE_ROOT"
        log "Removed cache directory: $CACHE_ROOT"
      else
        log "Cache directory not found: $CACHE_ROOT"
      fi
      ;;
    help|-h|--help)
      usage
      ;;
    *)
      warn "Unknown command: $cmd"
      usage
      exit 1
      ;;
  esac
}

main "$@"
```
