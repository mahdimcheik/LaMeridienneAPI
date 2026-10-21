# E04 — Remboursements

> Statut : **Stable** · Conventions : [README](README.md)

Un remboursement note qu'une somme a été rendue à un client. Il est **indépendant** des
crédits ([R-04](../20-specs-fonctionnelles/01-regles-metier.md#r-04--crédits-et-remboursements-sont-indépendants)) :
même forme de saisie, liste séparée, aucun rapprochement.

## US-E04-01 — Noter un remboursement

**En tant qu'** employé·e au comptoir
**je veux** noter qu'une somme a été rendue à un client
**afin de** pouvoir justifier l'écart de caisse en fin de journée.

**Priorité :** Doit · **Dépend de :** US-E02-01, US-E03-04

**État :** Absente

**Critères d'acceptation**

- Champs : client (obligatoire, autocomplétion de [US-E03-04](E03-credits-clients.md#us-e03-04--retrouver-un-client-par-autocomplétion)),
  montant (obligatoire), commentaire (facultatif, ex. « avoir », « erreur de rendu »).
- Étant donné la feuille d'un jour ouverte
  Quand je saisis un client et un montant puis je valide
  Alors la ligne apparaît dans la case Remboursements de ce jour, et **pas** dans les crédits.

- Étant donné un montant invalide ([R-05](../20-specs-fonctionnelles/01-regles-metier.md#r-05--montants))
  Quand je valide
  Alors la ligne est refusée avec un message qui désigne le champ.

## US-E04-02 — Corriger un remboursement

**En tant qu'** utilisateur
**je veux** modifier une ligne de remboursement
**afin de** rectifier une erreur de saisie.

**Priorité :** Doit · **Dépend de :** US-E04-01

**État :** Absente

**Critères d'acceptation**

- Mêmes champs et contrôles qu'à la création.
- Étant donné une ligne supprimée entre-temps
  Quand j'enregistre
  Alors la demande répond `404`.

## US-E04-03 — Supprimer un remboursement

**En tant qu'** utilisateur
**je veux** supprimer une ligne de remboursement
**afin de** retirer une saisie faite par erreur.

**Priorité :** Doit · **Dépend de :** US-E04-01

**État :** Absente

**Critères d'acceptation**

- Une confirmation est demandée.
- Suppression logique ([R-06](../20-specs-fonctionnelles/01-regles-metier.md#r-06--suppression-logique)).
- Étant donné une ligne déjà supprimée
  Quand je la supprime
  Alors la demande répond `404`.
