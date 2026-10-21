# ADR-0004 — Une feuille par date calendaire

**Statut :** Accepté
**Application :** Appliquée
**Date :** 2026-10-21

## Contexte

La page A4 remplace une feuille papier quotidienne. Il fallait choisir entre une feuille
continue, une feuille par semaine ou une feuille par jour.

## Décision

Une feuille par **date calendaire** du fuseau `Europe/Paris`, identifiée par sa date (clé
unique `DailySheet.Date`). Crédits, remboursements, ventes de journaux et notes sont rattachés
à une feuille. L'URL et l'API désignent une feuille par sa date (`/api/sheets/2026-10-21`),
pas par un identifiant technique. Règle détaillée :
[R-01](../20-specs-fonctionnelles/01-regles-metier.md#r-01--date-dune-feuille).

## Conséquences

**Ce qu'on gagne :** correspondance directe avec l'habitude papier ; historique naturel ;
retrouver un jour est trivial.

**Ce que ça coûte :** une saisie oubliée la veille doit être faite sur la feuille de la veille
(navigation par date) ; la création de la feuille doit résister aux accès simultanés (clé
unique + reprise sur conflit).

**Ce que ça interdit :** une ligne de crédit « sans jour ».
