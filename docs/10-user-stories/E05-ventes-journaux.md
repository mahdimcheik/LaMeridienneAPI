# E05 — Ventes de journaux

> Statut : **Stable** · Conventions : [README](README.md)

On compte des **quantités vendues** par titre et par jour. Pas de prix, pas de stock, pas
d'invendus ([Q5](../50-backlog/02-questions-ouvertes.md#q5--prix-des-journaux)).

## US-E05-01 — Compter les journaux vendus

**En tant qu'** employé·e au comptoir
**je veux** augmenter le compteur d'un journal à chaque vente, ou saisir directement la quantité
**afin de** connaître en fin de journée combien d'exemplaires de chaque titre sont partis.

**Priorité :** Doit · **Dépend de :** US-E02-01

**État :** Implémentée · **Code :** [`NewspaperSalesController`](../../Controllers/NewspaperSalesController.cs)

**Critères d'acceptation**

- La case affiche une ligne par journal actif, dans l'ordre défini en administration, avec
  sa quantité (0 par défaut).
- Boutons `+1` et `−1`, et saisie directe de la quantité.
- Étant donné une quantité négative, décimale ou au-delà de 999
  Quand je l'enregistre
  Alors elle est refusée et la valeur précédente est rétablie.
  Un `+1` à 999 est ignoré, comme un `−1` à 0.

- Étant donné deux postes qui font `+1` en même temps sur le même journal
  Quand les deux demandes arrivent
  Alors la quantité augmente de 2 ([R-09](../20-specs-fonctionnelles/01-regles-metier.md#r-09--incréments-concurrents)).

- Au premier démarrage, les journaux **Sud Ouest** et **Le Républicain** existent.

## US-E05-02 — Voir les journaux d'une feuille passée

**En tant que** gérant·e
**je veux** retrouver les quantités vendues d'un jour passé, y compris pour un titre retiré depuis
**afin de** comparer avec le relevé du distributeur.

**Priorité :** Doit · **Dépend de :** US-E05-01, US-E02-02

**État :** Implémentée · **Code :** [`SheetService`](../../Services/SheetService.cs)

**Critères d'acceptation**

- Étant donné un journal désactivé après le jour consulté
  Quand j'ouvre la feuille de ce jour
  Alors le journal y figure avec sa quantité
  ([R-10](../20-specs-fonctionnelles/01-regles-metier.md#r-10--journal-désactivé)).

- Étant donné un journal désactivé sans vente ce jour-là
  Quand j'ouvre la feuille
  Alors il n'apparaît pas.

- Étant donné un journal ajouté après le jour consulté
  Quand j'ouvre la feuille de ce jour
  Alors il apparaît avec 0 et reste saisissable.
