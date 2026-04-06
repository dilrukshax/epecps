# Run Project With Docker Dev Script

This guide runs backend, frontend, and SQL Server with one development-focused script.

## Default Development Flow

`./scripts/docker.sh up` is optimized for daily development:

- Uses only `db`, `backend`, and `frontend` services
- Does not force image pulls every run
- Uses cached Docker build layers
- Builds only changed layers/services
- Ensures `EpecpsDb` exists, then backend starts
- Backend runs migrations only when pending migrations exist
- Backend runs seed only when core seed tables are missing
- Excel test-data import runs automatically only when no existing `@empovate.test` users are found

## Script Location

- [docker.sh](/Users/dilandilaruksha/Project/epecps/scripts/docker.sh)

## Quick Start

From project root:

```bash
./scripts/docker.sh up
```

## Full Refresh (Only When Needed)

Use this only when you explicitly need to refresh all images/build layers:

```bash
./scripts/docker.sh up-fresh
```

## Useful Commands

```bash
# Rebuild all app services with cache
./scripts/docker.sh rebuild

# Rebuild one service with cache
./scripts/docker.sh rebuild backend
./scripts/docker.sh rebuild frontend

# Pull latest image updates + no-cache rebuild (without start)
./scripts/docker.sh refresh-images

# Import Excel test data manually
./scripts/docker.sh seed-test-data

# View status
./scripts/docker.sh ps

# Tail logs
./scripts/docker.sh logs
./scripts/docker.sh logs backend

# Stop stack
./scripts/docker.sh down

# Stop stack and remove DB volume
./scripts/docker.sh reset
```

## Access URLs

- Frontend: http://localhost:4200
- Backend API: http://localhost:8080

## Optional Flags

```bash
# Disable automatic Excel test-data import during "up"
IMPORT_TEST_DATA_ON_UP=false ./scripts/docker.sh up

# Use a custom Excel file for manual import
TEST_DATA_FILE=/absolute/path/file.xlsx ./scripts/docker.sh seed-test-data
```

## Full Script Code

```bash
#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

log() {
  printf '[docker-dev] %s\n' "$1"
}

warn() {
  printf '[docker-dev][warn] %s\n' "$1"
}

usage() {
  cat <<'EOF'
Usage: ./scripts/docker.sh [command] [args]

Commands:
  up               Development start (cached build, no forced pull)
  up-fresh         Full refresh (pull latest + rebuild with no-cache)
  refresh-images   Pull latest image updates + no-cache rebuild (backend/frontend only)
  rebuild [svc]    Rebuild all or one service using cache, then start
  seed-test-data   Import Excel test data into running backend
  down             Stop and remove running containers
  reset            Stop stack and remove SQL Server volume data
  ps               Show running services
  logs [service]   Tail logs for all services or one service
  help             Show this help text
EOF
}

ensure_env_file() {
  if [[ ! -f .env ]]; then
    if [[ -f .env.docker.example ]]; then
      cp .env.docker.example .env
      log "Created .env from .env.docker.example"
    else
      warn "Missing .env and .env.docker.example. Cannot continue."
      exit 1
    fi
  fi
}

get_env_value() {
  local key="$1"
  if [[ -f .env ]]; then
    local line
    line="$(grep -E "^${key}=" .env | head -n 1 || true)"
    if [[ -n "$line" ]]; then
      printf '%s' "${line#*=}"
      return 0
    fi
  fi

  return 1
}

is_true() {
  case "${1:-}" in
    1|true|TRUE|True|yes|YES|Yes|y|Y|on|ON|On)
      return 0
      ;;
    *)
      return 1
      ;;
  esac
}

ensure_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    warn "Docker is not installed. Install Docker Desktop first."
    exit 1
  fi

  if ! docker compose version >/dev/null 2>&1; then
    warn "Docker Compose plugin is not available. Install/enable Docker Compose v2."
    exit 1
  fi
}

ensure_db_image_available() {
  if docker image inspect mcr.microsoft.com/mssql/server:2022-latest >/dev/null 2>&1; then
    return
  fi

  log "SQL Server image is missing locally. Pulling once..."
  docker compose pull db
}

pull_images() {
  log "Pulling latest external images (db)..."
  docker compose pull db
}

rebuild_no_cache() {
  log "Rebuilding backend and frontend with --no-cache --pull..."
  docker compose build --no-cache --pull backend frontend
}

ensure_app_database() {
  local db_name="EpecpsDb"
  local sa_password

  sa_password="$(get_env_value MSSQL_SA_PASSWORD || true)"
  if [[ -z "$sa_password" ]]; then
    sa_password="${MSSQL_SA_PASSWORD:-YourStrong!Passw0rd}"
  fi

  local sql
  sql="IF DB_ID(N'${db_name}') IS NULL CREATE DATABASE [${db_name}];"

  for attempt in $(seq 1 30); do
    if docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$sa_password" -Q "$sql" >/dev/null 2>&1; then
      log "Ensured database exists: ${db_name}"
      return 0
    fi

    sleep 2
  done

  warn "Could not ensure database exists (${db_name}) before backend startup."
  return 1
}

wait_backend_ready() {
  local backend_base_url="${BACKEND_BASE_URL:-http://localhost:8080}"
  local ready_url="${backend_base_url%/}/swagger/v1/swagger.json"

  if ! command -v curl >/dev/null 2>&1; then
    warn "curl not found. Skipping backend readiness wait."
    return 0
  fi

  for _ in $(seq 1 60); do
    local code
    code="$(curl -s -o /dev/null -w '%{http_code}' "$ready_url" || true)"
    if [[ "$code" == "200" ]]; then
      return 0
    fi
    sleep 2
  done

  warn "Backend did not become ready in time: $ready_url"
  return 1
}

seed_test_data() {
  local test_data_file="${TEST_DATA_FILE:-$ROOT_DIR/docs/test-data/empovate360-import-users-v2.xlsx}"
  local backend_base_url="${BACKEND_BASE_URL:-http://localhost:8080}"
  local super_admin_email
  local super_admin_password
  local login_payload
  local login_response
  local access_token
  local import_http_code
  local import_response_file

  if [[ ! -f "$test_data_file" ]]; then
    warn "Test data file not found: $test_data_file (skipping import)"
    return 0
  fi

  if ! command -v curl >/dev/null 2>&1; then
    warn "curl not found. Cannot import test data."
    return 0
  fi

  if ! wait_backend_ready; then
    warn "Skipping test-data import because backend is not ready."
    return 0
  fi

  super_admin_email="$(get_env_value SUPER_ADMIN_EMAIL || true)"
  if [[ -z "$super_admin_email" ]]; then
    super_admin_email="${SUPER_ADMIN_EMAIL:-superadmin@company.com}"
  fi

  super_admin_password="$(get_env_value SUPER_ADMIN_PASSWORD || true)"
  if [[ -z "$super_admin_password" ]]; then
    super_admin_password="${SUPER_ADMIN_PASSWORD:-CHANGE_THIS_SUPERADMIN_PASSWORD}"
  fi

  login_payload="$(printf '{"email":"%s","password":"%s"}' "$super_admin_email" "$super_admin_password")"
  login_response="$(curl -sS -X POST "${backend_base_url%/}/api/v1/auth/login" -H "Content-Type: application/json" -d "$login_payload" || true)"
  access_token="$(printf '%s' "$login_response" | tr -d '\n' | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')"

  if [[ -z "$access_token" ]]; then
    warn "Could not login as SuperAdmin to import test data. Check SUPER_ADMIN_EMAIL/SUPER_ADMIN_PASSWORD."
    return 0
  fi

  import_response_file="$(mktemp)"
  import_http_code="$(curl -sS -o "$import_response_file" -w '%{http_code}' \
    -X POST "${backend_base_url%/}/api/v1/admin/import/users-projects" \
    -H "Authorization: Bearer ${access_token}" \
    -F "file=@${test_data_file}" || true)"

  if [[ "$import_http_code" =~ ^2 ]]; then
    log "Test data imported from: $test_data_file"
  else
    warn "Test-data import failed with HTTP ${import_http_code}."
    warn "Response: $(head -c 300 "$import_response_file" | tr '\n' ' ')"
  fi

  rm -f "$import_response_file"
}

seed_test_data_if_enabled() {
  local enabled="${IMPORT_TEST_DATA_ON_UP:-true}"
  if is_true "$enabled"; then
    seed_test_data
  else
    log "Skipping automatic test-data import (IMPORT_TEST_DATA_ON_UP=$enabled)"
  fi
}

up_dev() {
  ensure_db_image_available

  log "Starting database..."
  docker compose up -d db

  ensure_app_database

  log "Starting backend and frontend with cached build..."
  docker compose up -d --build backend frontend

  seed_test_data_if_enabled

  docker compose ps
  printf '\nFrontend: http://localhost:4200\nBackend:  http://localhost:8080\n\n'
}

up_fresh() {
  log "Stopping existing stack (if running)..."
  docker compose down --remove-orphans || true

  pull_images
  rebuild_no_cache

  log "Starting database..."
  docker compose up -d db

  ensure_app_database

  log "Starting backend and frontend..."
  docker compose up -d backend frontend

  seed_test_data_if_enabled

  docker compose ps
  printf '\nFrontend: http://localhost:4200\nBackend:  http://localhost:8080\n\n'
}

main() {
  local cmd="${1:-up}"

  case "$cmd" in
    help|-h|--help)
      usage
      exit 0
      ;;
  esac

  ensure_docker
  ensure_env_file

  case "$cmd" in
    up)
      up_dev
      ;;
    up-fresh)
      up_fresh
      ;;
    refresh-images)
      pull_images
      rebuild_no_cache
      ;;
    rebuild)
      if [[ -n "${2:-}" ]]; then
        log "Rebuilding service: $2 (cached build)"
        docker compose up -d --build "$2"
      else
        log "Rebuilding backend/frontend (cached build)"
        docker compose build backend frontend
        docker compose up -d db
        docker compose up -d backend frontend
        seed_test_data_if_enabled
      fi
      ;;
    seed-test-data)
      seed_test_data
      ;;
    down)
      docker compose down
      ;;
    reset)
      docker compose down -v --remove-orphans
      ;;
    ps)
      docker compose ps
      ;;
    logs)
      if [[ -n "${2:-}" ]]; then
        docker compose logs -f "$2"
      else
        docker compose logs -f
      fi
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
