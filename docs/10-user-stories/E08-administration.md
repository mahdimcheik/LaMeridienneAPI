# E08 — Administration

> Statut : **Stable** · Conventions : [README](README.md) ·
> Permissions : [rôles & permissions](../20-specs-fonctionnelles/02-roles-permissions.md)

Les trois comptes sont administrateurs : l'administration est accessible à tous.

## US-E08-01 — Gérer la liste des journaux

**En tant que** gérant·e
**je veux** ajouter, renommer, réordonner et désactiver les journaux
**afin que** la case Journaux corresponde aux titres réellement vendus.

**Priorité :** Doit · **Dépend de :** US-E05-01

**État :** Absente

**Critères d'acceptation**

- Étant donné un nom vide, ou identique (sans tenir compte de la casse) à un journal existant
  Quand j'ajoute ou je renomme
  Alors l'opération est refusée avec un message explicite.

- Un journal n'est **jamais supprimé**, seulement désactivé
  ([R-10](../20-specs-fonctionnelles/01-regles-metier.md#r-10--journal-désactivé)) ; il peut être réactivé.

- Le renommage s'applique aussi aux feuilles passées (c'est le même titre).

- L'ordre choisi est celui de la case Journaux sur toutes les feuilles.

## US-E08-02 — Gérer le référentiel clients

**En tant que** gérant·e
**je veux** corriger le nom d'un client et fusionner deux fiches qui désignent la même personne
**afin que** l'autocomplétion reste propre malgré les fautes de frappe.

**Priorité :** Pourrait · **Dépend de :** US-E03-04

**État :** Absente

**Critères d'acceptation**

- Liste paginée et recherche par nom.
- Renommer un client renomme toutes ses lignes et rendez-vous (c'est la même fiche).
- Étant donné deux clients A et B
  Quand je fusionne B dans A
  Alors toutes les lignes et rendez-vous de B passent à A
  Et B n'est plus proposé par l'autocomplétion.

- Étant donné un nom déjà porté par un autre client
  Quand je renomme
  Alors l'opération est refusée et la fusion est proposée.

## US-E08-03 — Changer mon mot de passe

**En tant qu'** utilisateur
**je veux** changer mon mot de passe
**afin de** remplacer le mot de passe initial fourni à l'installation.

**Priorité :** Devrait · **Dépend de :** US-E01-01

**État :** Absente

**Critères d'acceptation**

- Le mot de passe actuel est exigé.
- Étant donné un mot de passe actuel erroné
  Quand je valide
  Alors le changement est refusé.

- Étant donné un nouveau mot de passe trop faible ([R-14](../20-specs-fonctionnelles/01-regles-metier.md#r-14--mot-de-passe))
  Quand je valide
  Alors le changement est refusé avec la règle non respectée.

- Après changement, toutes mes autres sessions sont révoquées ; la session courante reste ouverte.
