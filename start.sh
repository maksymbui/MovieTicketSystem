#!/bin/bash
SCRIPT_PATH="$(realpath "${BASH_SOURCE[0]}")"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_DIR="$ROOT_DIR/MovieTicketSystem"

if [[ "${MOVIETICKETS_DEVENV:-0}" != "1" ]]; then
  exec nix develop "$APP_DIR" --command env MOVIETICKETS_DEVENV=1 "$SCRIPT_PATH" "$@"
fi

BACKEND_DIR="$APP_DIR/backend"
API_PROJECT="$BACKEND_DIR/src/MovieTickets.Api/MovieTickets.Api.csproj"
FRONTEND_DIR="$APP_DIR/frontend"
DESKTOP_DIR="$APP_DIR/desktop"

echo "▶ Publishing ASP.NET backend..."
dotnet publish "$API_PROJECT" -c Release

echo "▶ Building React frontend..."
pushd "$FRONTEND_DIR" >/dev/null
npm install
npm run build
popd >/dev/null

echo "▶ Compiling Electron main process..."
pushd "$DESKTOP_DIR" >/dev/null
npm install
npm run compile
echo "▶ Starting Electron app..."
npm run start:prod
popd >/dev/null