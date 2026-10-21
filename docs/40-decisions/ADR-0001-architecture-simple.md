# ADR-0001 — Architecture simple

**Statut :** Accepté
**Application :** À faire
**Date :** 2026-10-21

## Contexte

L'application sert trois personnes dans une boutique. Le volume est minime (quelques dizaines
de lignes par jour). Le besoin exprimé est « une base PostgreSQL simple, sans complexité ».
BonProfAPI a montré le coût d'une structure lourde à faire évoluer.

## Décision

- Un seul projet ASP.NET Core `net10.0`, contrôleurs minces, services métier, `AppDbContext`
  utilisé directement par les services.
- EF Core + Npgsql, migrations EF versionnées dans le dépôt.
- Ni dépôt générique, ni médiateur, ni CQRS, ni cache, ni file de messages.

Détails : [architecture](../30-specs-techniques/01-architecture.md).

## Conséquences

**Ce qu'on gagne :** peu de code, lecture directe, démarrage rapide, une seule dépendance
d'infrastructure.

**Ce que ça coûte :** les services dépendent d'EF Core ; les tests d'intégration ont besoin
d'un vrai PostgreSQL (Testcontainers), ce qui est voulu pour tester les contraintes d'unicité.

**À surveiller :** si un service dépasse ~300 lignes, le découper par cas d'usage plutôt que
d'ajouter une couche.
