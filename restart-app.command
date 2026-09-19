#!/bin/bash
# Ada-ncoa — double-click this file after code changes: it rebuilds and restarts the app.
cd "$(dirname "$0")" || exit 1

echo "==> Reconstruiesc și repornesc Ada-ncoa..."
echo "    Când vezi 'Now listening on: http://[::]:8080', deschide http://localhost:8080"
echo

if ! docker info >/dev/null 2>&1; then
  echo "!! Docker Desktop nu rulează. Pornește Docker Desktop și încearcă din nou."
  read -r -p "Apasă Enter pentru a închide..."
  exit 1
fi

docker compose down
docker compose up --build

echo
echo "==> Aplicația s-a oprit."
read -r -p "Apasă Enter pentru a închide fereastra..."
