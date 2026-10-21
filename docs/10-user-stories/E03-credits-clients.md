# E03 — Crédits clients

> Statut : **Stable** · Conventions : [README](README.md)

Un crédit note qu'un client a emporté sans payer. **Ce n'est pas une créance comptable** :
aucun solde n'est calculé ([R-04](../20-specs-fonctionnelles/01-regles-metier.md#r-04--crédits-et-remboursements-sont-indépendants)).

## US-E03-01 — Noter un crédit client

**En tant qu'** employé·e au comptoir
**je veux** noter qu'un client emporte sans payer, avec le montant
**afin de** savoir plus tard qui doit quoi.

**Priorité :** Doit · **Dépend de :** US-E02-01, US-E03-04

**État :** Absente

**Critères d'acceptation**

- Champs : client (obligatoire), montant (obligatoire), commentaire (facultatif).
- Étant donné la feuille d'un jour ouverte
  Quand je saisis un client et un montant puis je valide
  Alors la ligne apparaît dans la case Crédits clients de ce jour
  Et elle est toujours là après rechargement de la page.

- Étant donné un montant vide, nul, négatif ou au-delà de la limite
  ([R-05](../20-specs-fonctionnelles/01-regles-metier.md#r-05--montants))
  Quand je valide
  Alors la ligne est refusée avec un message qui désigne le champ.

- Étant donné un client vide
  Quand je valide
  Alors la ligne est refusée.

- La ligne porte son auteur et son heure de saisie ([US-E09-03](E09-transverse.md#us-e09-03--savoir-qui-a-noté-quoi)).

## US-E03-02 — Corriger un crédit

**En tant qu'** utilisateur
**je veux** modifier une ligne de crédit déjà saisie
**afin de** rectifier une erreur de montant ou de nom sans tout ressaisir.

**Priorité :** Doit · **Dépend de :** US-E03-01

**État :** Absente

**Critères d'acceptation**

- Tous les champs sont modifiables, avec les mêmes contrôles qu'à la création.
- Étant donné une ligne supprimée entre-temps par un autre poste
  Quand j'enregistre ma modification
  Alors la demande répond `404` et un message propose de recharger la feuille.

- La ligne garde son auteur d'origine et porte en plus l'auteur et l'heure de la dernière
  modification.

## US-E03-03 — Supprimer un crédit

**En tant qu'** utilisateur
**je veux** supprimer une ligne de crédit
**afin de** retirer une saisie faite par erreur ou un crédit réglé.

**Priorité :** Doit · **Dépend de :** US-E03-01

**État :** Absente

**Critères d'acceptation**

- Une confirmation est demandée avant suppression.
- Étant donné une ligne déjà supprimée
  Quand je la supprime à nouveau
  Alors la demande répond `404`, sans erreur visible autre qu'un rechargement de la case.

- La suppression est logique ([R-06](../20-specs-fonctionnelles/01-regles-metier.md#r-06--suppression-logique)).

## US-E03-04 — Retrouver un client par autocomplétion

**En tant qu'** employé·e au comptoir
**je veux** que les noms de clients déjà saisis me soient proposés pendant la frappe
**afin de** saisir vite et d'écrire toujours le même nom de la même façon.

**Priorité :** Doit

**État :** Absente

**Critères d'acceptation**

- Étant donné au moins deux caractères tapés
  Quand je saisis le nom
  Alors au plus 10 clients dont le nom **contient** ces caractères sont proposés, sans tenir
  compte des majuscules ni des accents.

- Étant donné un nom qui ne correspond à aucun client
  Quand je valide la ligne
  Alors le client est créé à la volée ([R-08](../20-specs-fonctionnelles/01-regles-metier.md#r-08--référentiel-clients)).

- La liste de propositions se parcourt au clavier (flèches, Entrée, Échap).

- Le même référentiel sert aux crédits, aux remboursements et aux rendez-vous pneus.

## US-E03-05 — Voir les crédits d'un client

**En tant que** gérant·e
**je veux** voir toutes les lignes de crédit d'un client, toutes dates confondues
**afin de** lui dire ce qui est noté à son nom quand il passe régler.

**Priorité :** Devrait · **Dépend de :** US-E03-04

**État :** Absente

**Critères d'acceptation**

- Les lignes sont listées de la plus récente à la plus ancienne, avec leur date de feuille.
- Un lien ouvre la feuille du jour de chaque ligne.
- Aucun total n'est présenté comme un solde dû
  ([R-04](../20-specs-fonctionnelles/01-regles-metier.md#r-04--crédits-et-remboursements-sont-indépendants)).
- Étant donné un client inconnu
  Quand je demande ses crédits
  Alors la demande répond `404`.
