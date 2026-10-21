# ADR-0003 — Trois comptes pré-créés, sans inscription

**Statut :** Accepté
**Application :** À faire
**Date :** 2026-10-21

## Contexte

Trois personnes utilisent l'application ; il n'y en aura pas d'autres à court terme. Un écran
d'inscription serait une surface d'attaque sans usage.

## Décision

- Les trois comptes sont créés au démarrage, **s'ils n'existent pas**, depuis la configuration
  (`Seed:Users:0..2`). Un compte existant n'est jamais modifié par le seed.
- Les trois comptes portent le rôle `Admin` et ont les mêmes droits
  ([rôles & permissions](../20-specs-fonctionnelles/02-roles-permissions.md)).
- Aucun point d'accès ne crée ni ne supprime de compte.
- L'API refuse de démarrer sur une base sans comptes si la configuration est incomplète.

## Conséquences

**Ce qu'on gagne :** aucun parcours d'inscription, de confirmation d'e-mail ni de mot de passe
oublié à construire et sécuriser.

**Ce que ça coûte :** un mot de passe oublié se réinitialise par un administrateur système
(mise à jour en base ou commande dédiée) ; changer un compte demande une intervention.

**À surveiller :** si une quatrième personne arrive, ajouter une commande d'administration
plutôt qu'un écran.
