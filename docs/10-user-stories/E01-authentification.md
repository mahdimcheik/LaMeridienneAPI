# E01 — Authentification

> Statut : **Stable** · Conventions : [README](README.md) ·
> Décisions : [ADR-0002](../40-decisions/ADR-0002-jwt-et-refresh-token.md),
> [ADR-0003](../40-decisions/ADR-0003-comptes-pre-crees.md)

Il n'y a **pas d'inscription** : les trois comptes existent dès le premier démarrage.

## US-E01-01 — Me connecter

**En tant qu'** utilisateur
**je veux** me connecter avec mon identifiant et mon mot de passe
**afin d'** accéder aux feuilles et à l'agenda, que personne d'extérieur ne doit lire.

**Priorité :** Doit · **Dépend de :** US-E01-04

**État :** Absente

**Critères d'acceptation**

- Étant donné un identifiant et un mot de passe valides
  Quand je me connecte
  Alors je reçois un jeton d'accès et un jeton de renouvellement
  Et je peux appeler les autres points d'accès.

- Étant donné un mot de passe erroné ou un identifiant inconnu
  Quand je me connecte
  Alors la connexion est refusée avec le **même** message dans les deux cas.

- Étant donné un compte verrouillé ([R-02](../20-specs-fonctionnelles/01-regles-metier.md#r-02--verrouillage))
  Quand je me connecte, même avec le bon mot de passe
  Alors la connexion est refusée et le message indique d'attendre.

- Tout point d'accès autre que la connexion et le renouvellement répond `401` sans jeton
  d'accès valide.

## US-E01-02 — Rester connecté

**En tant qu'** utilisateur
**je veux** que ma session se prolonge sans ressaisir mon mot de passe
**afin de** ne pas être interrompu au milieu d'une saisie devant un client.

**Priorité :** Doit · **Dépend de :** US-E01-01

**État :** Absente

**Critères d'acceptation**

- Étant donné un jeton de renouvellement valide
  Quand je demande un renouvellement
  Alors je reçois un nouveau jeton d'accès **et** un nouveau jeton de renouvellement
  Et l'ancien jeton de renouvellement ne fonctionne plus.

- Étant donné un jeton de renouvellement déjà utilisé
  Quand il est présenté à nouveau
  Alors le renouvellement est refusé
  Et toutes les sessions de ce compte sont révoquées ([R-03](../20-specs-fonctionnelles/01-regles-metier.md#r-03--réutilisation-dun-jeton-de-renouvellement)).

- Étant donné un jeton de renouvellement expiré ou révoqué
  Quand je demande un renouvellement
  Alors la demande répond `401`.

## US-E01-03 — Me déconnecter

**En tant qu'** utilisateur
**je veux** me déconnecter
**afin qu'** un poste partagé ne laisse pas ma session ouverte au suivant.

**Priorité :** Doit · **Dépend de :** US-E01-01

**État :** Absente

**Critères d'acceptation**

- Étant donné une session ouverte
  Quand je me déconnecte
  Alors mon jeton de renouvellement est révoqué et le cookie est effacé.

- Étant donné une session déjà fermée
  Quand je me déconnecte à nouveau
  Alors la demande réussit sans erreur (opération idempotente).

## US-E01-04 — Disposer des comptes dès l'installation

**En tant que** gérant·e
**je veux** que les trois comptes existent dès le premier démarrage
**afin de** mettre l'application en service sans écran d'inscription à sécuriser.

**Priorité :** Doit

**État :** Absente

**Critères d'acceptation**

- Étant donné une base vide et les identifiants des trois comptes fournis par la configuration
  Quand l'API démarre
  Alors les trois comptes existent, avec des mots de passe hachés.

- Étant donné des comptes déjà créés
  Quand l'API redémarre
  Alors aucun compte n'est dupliqué et aucun mot de passe n'est écrasé.

- Étant donné une configuration où un identifiant ou un mot de passe manque
  Quand l'API démarre sur une base vide
  Alors elle refuse de démarrer avec un message qui nomme la variable manquante.

- Il n'existe aucun point d'accès d'inscription.
