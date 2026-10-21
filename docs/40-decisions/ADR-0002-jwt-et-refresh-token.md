# ADR-0002 — Jeton d'accès JWT et jeton de renouvellement rotatif

**Statut :** Accepté
**Application :** Appliquée
**Date :** 2026-10-21

## Contexte

Le besoin est une authentification « simple avec token et refresh token ». Le client est une
application Angular servie à part en développement. Les postes de la boutique peuvent être
partagés ; il faut pouvoir couper une session.

## Décision

- **Jeton d'accès** : JWT signé, 15 minutes, transmis dans l'en-tête `Authorization: Bearer`,
  renvoyé dans le corps de la réponse et gardé **en mémoire** par le client.
- **Jeton de renouvellement** : valeur aléatoire opaque, stockée **hachée** en base, transmise
  uniquement dans un cookie `HttpOnly; Secure; SameSite=Strict; Path=/api/auth`, **rotatif** et
  à usage unique, avec détection de réutilisation.

Valeurs et mécanismes : [sécurité](../30-specs-techniques/04-securite.md).

## Conséquences

**Ce qu'on gagne :** le secret longue durée n'est jamais lisible par le JavaScript de la page ;
la déconnexion et le changement de mot de passe révoquent réellement ; pas de serveur de
session.

**Ce que ça coûte :** à chaque rechargement de page, le client appelle `refresh` avant tout
autre appel ; un jeton d'accès volé reste valable jusqu'à 15 minutes.

**Ce que ça interdit :** stocker un jeton en `localStorage` ; accepter le jeton de
renouvellement dans le corps d'une requête.
