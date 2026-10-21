#!/usr/bin/env bash
# Répétition de restauration : restaure une sauvegarde (la plus récente par défaut) dans une
# base temporaire, la compare à la base de production, puis supprime la base temporaire.
# Usage (racine du dépôt) : deploy/check-restore.sh [fichier dans deploy/backups]
set -euo pipefail
cd "$(dirname "$0")/.."
file="${1:-$(ls -1 deploy/backups/lameridienne-*.dump 2>/dev/null | sort | tail -n1 | xargs -r basename)}"
[ -n "$file" ] && [ -f "deploy/backups/$file" ] || { echo "Aucune sauvegarde dans deploy/backups." >&2; exit 1; }
check=lameridienne_restore_check
in_backup() { docker compose -f deploy/compose.prod.yaml exec -T backup "$@"; }
count() { in_backup psql -d "$1" -tAc "SELECT count(*) FROM \"$2\""; }

in_backup dropdb --if-exists "$check"
in_backup createdb "$check"
trap 'in_backup dropdb --if-exists "$check"' EXIT
in_backup pg_restore --no-owner --exit-on-error --dbname="$check" "/backups/$file"

rc=0
printf '%-22s %10s %10s\n' "Table ($file)" restaurée production
for table in __EFMigrationsHistory Users DailySheets Customers CreditEntries RefundEntries \
             Newspapers NewspaperSales TireAppointments; do
  restored=$(count "$check" "$table"); live=$(count lameridienne "$table"); mark=""
  if [ "$restored" != "$live" ]; then
    case "$table" in
      __EFMigrationsHistory|Users) mark="  ✗"; rc=1 ;;
      *) mark="  (saisies depuis la sauvegarde ?)" ;;
    esac
  fi
  printf '%-22s %10s %10s%s\n' "$table" "$restored" "$live" "$mark"
done

[ $rc -eq 0 ] && echo "✓ Restauration vérifiée." || echo "✗ La sauvegarde ne correspond pas au schéma ou aux comptes."
exit $rc
