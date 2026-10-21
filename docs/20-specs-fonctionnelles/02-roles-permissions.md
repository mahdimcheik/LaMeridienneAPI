# Rôles & permissions

> Statut : **Stable**

Il n'existe qu'**un rôle** : `Admin`. Les trois comptes le portent
([ADR-0003](../40-decisions/ADR-0003-comptes-pre-crees.md)).

| Opération | Anonyme | Admin |
|---|---|---|
| Se connecter, renouveler la session, se déconnecter | ✓ (sa propre session) | ✓ |
| Changer son mot de passe | — | ✓ (son compte) |
| Lire et modifier toute feuille, ligne, vente, note | — | ✓ |
| Lire et modifier tout rendez-vous | — | ✓ |
| Gérer journaux et clients | — | ✓ |
| Modifier la disposition | — | ✓ (la sienne) |
| Créer, supprimer un compte | — | — (configuration uniquement) |

Les données sont **partagées** : tout utilisateur voit et modifie les saisies des autres.
Seules la disposition des cases et les sessions sont personnelles.

Le rôle est tout de même porté dans le jeton et vérifié par une politique d'autorisation
(`[Authorize(Policy = "Admin")]` par défaut sur tous les contrôleurs), pour qu'un éventuel rôle
restreint plus tard ne demande pas de reprendre chaque point d'accès.
