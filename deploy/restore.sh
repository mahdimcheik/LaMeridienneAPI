#!/usr/bin/env bash
# Restaure une sauvegarde dans la base de production, API arrêtée pendant l'opération.
# Usage (racine du dépôt) : deploy/restore.sh lameridienne-2026-10-21.dump
set -euo pipefail
cd "$(dirname "$0")/.."
file="${1:?Usage : deploy/restore.sh <fichier dans deploy/backups>}"
[ -f "deploy/backups/$file" ] || { echo "Introuvable : deploy/backups/$file" >&2; exit 1; }
compose() { docker compose -f deploy/compose.prod.yaml "$@"; }

read -rp "Remplacer la base de production par $file ? (oui/non) " answer
[ "$answer" = "oui" ] || exit 1

compose stop api
compose exec -T backup pg_restore --clean --if-exists --no-owner --dbname=lameridienne "/backups/$file"
compose start api
echo "Base restaurée depuis $file."
