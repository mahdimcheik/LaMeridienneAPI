# User stories — conventions d'écriture

> Statut : **Stable**

## Identifiant

`US-Exx-nn` — `Exx` désigne l'épique, `nn` le numéro dans l'épique. **Un identifiant ne se
réutilise jamais**, même si la story est supprimée : il est cité dans les commits, les
tests et les tickets.

## Format

```markdown
## US-E03-01 — Noter un crédit client

**En tant qu'** employé·e au comptoir
**je veux** noter qu'un client emporte sans payer
**afin de** savoir plus tard qui doit quoi.

**Priorité :** Doit · **Dépend de :** US-E02-01

**État :** Absente

**Critères d'acceptation**

- Étant donné la feuille du jour ouverte
  Quand je saisis un client et un montant puis je valide
  Alors la ligne apparaît dans la case Crédits clients
  Et elle est toujours là après rechargement de la page.

- Étant donné un montant vide ou négatif
  Quand je valide
  Alors la ligne est refusée avec un message explicite.
```

Quand du code existe, la ligne d'état devient
`**État :** Partielle · **Code :** ` suivi d'un lien relatif vers le fichier concerné
(par exemple `Controllers/CreditsController.cs`), puis des écarts sur la ligne suivante.

## Règles de rédaction

1. **Le « afin de » est obligatoire** et doit porter une valeur, pas une paraphrase.
2. **Un critère d'acceptation est testable.** S'il ne peut pas devenir un test automatisé
   ou une manipulation reproductible, il est mal écrit.
3. **Les cas d'erreur font partie de la story.** Une story sans cas négatif est incomplète.
4. **Aucune règle n'est écrite ici.** On renvoie vers
   [les règles métier](../20-specs-fonctionnelles/01-regles-metier.md) par leur identifiant
   (`R-xx`). Une règle dupliquée est une règle qui divergera.
5. **Pas de solution technique** dans la story. Le « comment » est dans
   [les specs techniques](../30-specs-techniques/).
6. **L'état dit où en est le code**, jamais ce qu'il devrait faire : `Implémentée` si tous
   les critères côté API sont tenus, `Partielle` avec la liste des écarts, `Absente`, ou
   `Hors API` si la story relève entièrement du front (mise en page, impression…).

## Priorités (MoSCoW)

| Niveau | Sens |
|---|---|
| **Doit** | Sans elle, l'application ne remplace pas le papier. Bloque la mise en service. |
| **Devrait** | Fortement attendue, mais on peut démarrer sans. |
| **Pourrait** | Confort, à faire si le temps le permet. |

## Épiques

| Épique | Sujet |
|---|---|
| [E01](E01-authentification.md) | Authentification |
| [E02](E02-feuille-du-jour.md) | Feuille du jour |
| [E03](E03-credits-clients.md) | Crédits clients |
| [E04](E04-remboursements.md) | Remboursements |
| [E05](E05-ventes-journaux.md) | Ventes de journaux |
| [E06](E06-notes-generales.md) | Notes générales |
| [E07](E07-rendez-vous-pneus.md) | Rendez-vous pneus |
| [E08](E08-administration.md) | Administration |
| [E09](E09-transverse.md) | Transverse |
