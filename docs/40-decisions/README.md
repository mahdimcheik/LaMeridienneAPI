# Décisions d'architecture

> Statut : **Stable**

Un ADR (*Architecture Decision Record*) consigne **une** décision structurante : son
contexte, ce qui a été décidé, et ce que cela coûte. Il n'est jamais réécrit après coup —
une décision qui change donne lieu à un nouvel ADR qui remplace l'ancien.

Une décision peut être **acceptée** sans être encore **appliquée**. La ligne *Application* dit
où en est le code ; elle, et elle seule, est mise à jour au fil de la construction.

## Format

```markdown
# ADR-00xx — Titre

**Statut :** Proposé · Accepté · Remplacé par ADR-00yy
**Application :** Appliquée · Partielle · À faire
**Date :** AAAA-MM-JJ

## Contexte
Le problème, les contraintes, ce qui a été essayé.

## Décision
Ce qui est décidé, à l'affirmatif.

## Conséquences
Ce que ça coûte, ce que ça interdit, ce qu'il faudra surveiller.
```

## Index

| ADR | Titre | Statut | Application |
|---|---|---|---|
| [ADR-0001](ADR-0001-architecture-simple.md) | Architecture simple : un projet, contrôleurs, EF Core, PostgreSQL | Accepté | À faire |
| [ADR-0002](ADR-0002-jwt-et-refresh-token.md) | Jeton d'accès JWT et jeton de renouvellement rotatif en cookie | Accepté | À faire |
| [ADR-0003](ADR-0003-comptes-pre-crees.md) | Trois comptes pré-créés, sans inscription | Accepté | À faire |
| [ADR-0004](ADR-0004-feuille-par-jour.md) | Une feuille par date calendaire | Accepté | À faire |
