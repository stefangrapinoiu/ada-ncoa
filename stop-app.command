#!/bin/bash
# Ada-ncoa — double-click this file to stop the app.
cd "$(dirname "$0")" || exit 1

echo "==> Opresc Ada-ncoa..."
docker compose down
echo
echo "==> Aplicația a fost oprită. Datele au fost păstrate."
read -r -p "Apasă Enter pentru a închide fereastra..."
