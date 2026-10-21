#!/usr/bin/env bash
# Service « backup » : un pg_dump par jour à BACKUP_TIME (heure locale, TZ), conservé
# BACKUP_RETENTION_DAYS jours dans /backups. Avec --now : une sauvegarde immédiate, puis sortie.
set -euo pipefail

dump() {
  local file="/backups/lameridienne-$(date +%F).dump"
  pg_dump --format=custom --file="$file.tmp"
  mv "$file.tmp" "$file"
  find /backups -name 'lameridienne-*.dump' -mtime +"${BACKUP_RETENTION_DAYS:-30}" -delete
  echo "$(date -Iseconds) sauvegarde écrite : $file ($(du -h "$file" | cut -f1))"
}

if [ "${1:-}" = "--now" ]; then dump; exit 0; fi

time="${BACKUP_TIME:-03:00}"
echo "Sauvegarde quotidienne à $time ($TZ), conservée ${BACKUP_RETENTION_DAYS:-30} jours."
while true; do
  next=$(date -d "today $time" +%s)
  [ "$next" -le "$(date +%s)" ] && next=$(date -d "tomorrow $time" +%s)
  sleep $((next - $(date +%s)))
  dump || echo "$(date -Iseconds) ÉCHEC de la sauvegarde" >&2
done
