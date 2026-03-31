#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

usage() {
  cat <<'EOF'
Usage: ./scripts/docker.sh [command] [args]

Commands:
  up          Build and start all services in Docker (detached)
  up-fast     Start all services without rebuilding
  down        Stop and remove running containers
  reset       Stop containers and remove SQL Server volume data
  ps          Show running services
  logs [svc]  Tail logs for all services or one service
  migrate     Run backend migration profile once
EOF
}

ensure_env_file() {
  if [[ ! -f .env ]]; then
    if [[ -f .env.docker.example ]]; then
      cp .env.docker.example .env
      echo "Created .env from .env.docker.example"
    else
      echo "Missing .env and .env.docker.example. Cannot continue."
      exit 1
    fi
  fi
}

ensure_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is not installed. Install Docker Desktop first."
    exit 1
  fi
}

main() {
  local cmd="${1:-up}"
  ensure_docker
  ensure_env_file

  case "$cmd" in
    up)
      docker compose up --build -d
      ;;
    up-fast)
      docker compose up -d
      ;;
    down)
      docker compose down
      ;;
    reset)
      docker compose down -v
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
    migrate)
      docker compose --profile migrate run --rm backend-migrator
      ;;
    *)
      usage
      exit 1
      ;;
  esac
}

main "$@"
