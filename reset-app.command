#!/bin/bash
# Ada-ncoa — double-click to rebuild the app AND recreate the database from scratch.
# Use this after a change to the data model. All existing data is deleted;
# the sample users and sample food items are recreated.
cd "$(dirname "$0")" || exit 1

echo "==> ATENȚIE: baza de date va fi ștearsă și recreată."
read -r -p "Continui? (da/nu): " answer
case "$answer" in
  da|DA|Da|d|y|yes) ;;
  *) echo "Anulat."; exit 0 ;;
esac

if ! docker info >/dev/null 2>&1; then
  echo "!! Docker Desktop nu rulează. Pornește Docker Desktop și încearcă din nou."
  read -r -p "Apasă Enter pentru a închide..."
  exit 1
fi

docker compose down -v
docker compose up --build

echo
echo "==> Aplicația s-a oprit."
read -r -p "Apasă Enter pentru a închide fereastra..."
