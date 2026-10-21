#!/usr/bin/env bash
# Contrôles de cohérence de la documentation.
# Usage : ./docs/check-docs.sh

set -uo pipefail
cd "$(dirname "$0")/.." || exit 1
DOCS=docs
rc=0
ko() { echo "  ✗ $*"; rc=1; }
ok() { echo "  ✓ $*"; }

echo "── 1. Liens relatifs ──"
broken=0
while IFS='|' read -r src target; do
  [ -z "$target" ] && continue
  [ -e "$(dirname "$src")/$target" ] || { ko "lien cassé : $src → $target"; broken=1; }
done < <(grep -roEn '\]\([^)#h][^)]*\)' --include='*.md' "$DOCS" \
         | sed -E 's/^([^:]+):[0-9]+:\]\(([^)#]*)[^)]*\)$/\1|\2/')
[ $broken -eq 0 ] && ok "tous les liens résolvent"

echo "── 2. Blocs de code équilibrés ──"
bad=0
while read -r f; do
  n=$(grep -c '^```' "$f"); [ $((n % 2)) -ne 0 ] && { ko "fences déséquilibrés : $f"; bad=1; }
done < <(grep -rl '```' --include='*.md' "$DOCS")
[ $bad -eq 0 ] && ok "tous les blocs de code sont fermés"

echo "── 3. User stories ──"
dup=$(grep -rhoE '^#{2,3} US-E[0-9]{2}-[0-9]+[a-z]?' "$DOCS/10-user-stories"/E*.md | awk '{print $2}' | sort | uniq -d)
[ -n "$dup" ] && ko "identifiants dupliqués : $dup" || ok "aucun identifiant dupliqué"
for f in "$DOCS/10-user-stories"/E*.md; do
  s=$(grep -cE '^#{2,3} US-' "$f"); c=$(grep -c "^\*\*Critères d'acceptation\*\*" "$f")
  [ "$s" -ne "$c" ] && ko "$(basename "$f") : $s stories mais $c blocs de critères"
  e=$(grep -cE '^\*\*État :\*\* (Implémentée|Partielle|Absente|Hors API)' "$f")
  [ "$s" -ne "$e" ] && ko "$(basename "$f") : $s stories mais $e lignes d'état valides"
done
[ $rc -eq 0 ] && ok "chaque story porte des critères d'acceptation et un état"
echo "  → $(grep -rhcE '^#{2,3} US-' "$DOCS/10-user-stories"/E*.md | paste -sd+ | bc) user stories"

echo "── 4. ADR référencés ──"
for a in "$DOCS/40-decisions"/ADR-*.md; do
  n=$(basename "$a")
  c=$(grep -rl "$n" "$DOCS" --include='*.md' | grep -vc "40-decisions/$n")
  [ "$c" -eq 0 ] && ko "$n n'est référencé par aucune spec"
  grep -qE '^\*\*Application :\*\* (Appliquée|Partielle|À faire)' "$a" || ko "$n : ligne Application absente ou invalide"
done
ok "contrôle des références ADR effectué"

echo "── 5. Diagrammes mermaid ──"
if command -v npx >/dev/null 2>&1; then
  tmp=$(mktemp -d); i=0
  while read -r f; do
    awk '/^```mermaid$/{c=1;next} /^```$/{if(c){c=0;print "\f"}} c' "$f"
  done < <(grep -rl '^```mermaid' --include='*.md' "$DOCS") | csplit -sz -f "$tmp/d" -b '%02d.mmd' - '/\f/' '{*}' 2>/dev/null
  for m in "$tmp"/*.mmd; do
    [ -e "$m" ] || continue
    sed -i '/^\f$/d' "$m"
    [ -s "$m" ] || continue          # chunk vide produit par csplit
    i=$((i+1))
    npx -y @mermaid-js/mermaid-cli@11 -i "$m" -o "$m.svg" >/dev/null 2>&1 || ko "diagramme invalide dans $m"
  done
  ok "$i diagrammes vérifiés"; rm -rf "$tmp"
else
  echo "  – npx absent, contrôle ignoré"
fi

echo "── 6. Couverture des contrôleurs ──"
C="$DOCS/30-specs-techniques/03-contrat-api.md"; n=0; miss=0
for f in Controllers/*.cs; do
  b=$(basename "$f" .cs)
  grep -q -- "$b" "$C" || { ko "$b absent du contrat d'API"; miss=1; }
  n=$((n+1))
done
[ $miss -eq 0 ] && ok "les $n fichiers de Controllers sont tous traités"

echo
[ $rc -eq 0 ] && echo "✓ Documentation cohérente." || echo "✗ Des contrôles ont échoué."
exit $rc
