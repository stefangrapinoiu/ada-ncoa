#!/bin/bash
# Dă Mai Departe — double-click this file to start the app.
# It builds and runs the app and the database in Docker.
cd "$(dirname "$0")" || exit 1

echo "==> Pornesc Ada-ncoa..."
echo "    Prima rulare poate dura 5-10 minute."
echo "    Când vezi 'Now listening on: http://[::]:8080', deschide http://localhost:8080"
echo

if ! docker info >/dev/null 2>&1; then
  echo "!! Docker Desktop nu rulează. Pornește Docker Desktop și încearcă din nou."
  echo
  read -r -p "Apasă Enter pentru a închide..."
  exit 1
fi

docker compose up --build

echo
echo "==> Aplicația s-a oprit."
read -r -p "Apasă Enter pentru a închide fereastra..."
