# E09 — Transverse

> Statut : **Stable** · Conventions : [README](README.md)

## US-E09-01 — Utiliser l'application sans souris ni bonne vue

**En tant qu'** utilisateur
**je veux** une interface accessible au clavier et lisible
**afin de** saisir vite au comptoir et de ne laisser personne de côté.

**Priorité :** Doit

**État :** Hors API

**Critères d'acceptation**

- Toutes les pages passent les contrôles axe sans erreur.
- Contrastes et focus conformes WCAG 2.2 niveau AA.
- Chaque action (saisie, suppression, déplacement de rendez-vous, redimensionnement de case)
  a un équivalent clavier.

## US-E09-02 — Ne jamais perdre une saisie

**En tant qu'** utilisateur
**je veux** que mes saisies s'enregistrent seules et que je sache quand c'est fait
**afin de** ne pas perdre une ligne si je ferme l'onglet ou si le réseau coupe.

**Priorité :** Doit

**État :** Implémentée · **Code :** [`ProblemDetailsSetup`](../../Infrastructure/ProblemDetailsSetup.cs)

**Critères d'acceptation**

- Un indicateur montre l'état : enregistré, en cours, échec.
- Étant donné une coupure réseau
  Quand l'enregistrement échoue
  Alors la saisie reste à l'écran, l'indicateur passe en échec et un bouton permet de réessayer.

- Étant donné une saisie non enregistrée
  Quand je quitte la page
  Alors le navigateur demande confirmation.

- Les erreurs de l'API sont au format `ProblemDetails` avec un message lisible en français
  ([contrat d'API](../30-specs-techniques/03-contrat-api.md)).

## US-E09-03 — Savoir qui a noté quoi

**En tant que** gérant·e
**je veux** voir l'auteur et l'heure de chaque ligne
**afin de** demander des précisions à la bonne personne.

**Priorité :** Devrait

**État :** Implémentée · **Code :** [`AuditInterceptor`](../../Data/AuditInterceptor.cs)

**Critères d'acceptation**

- Chaque ligne (crédit, remboursement, rendez-vous) porte l'auteur et l'heure de création, et
  ceux de la dernière modification s'il y en a une.
- Ces informations sont posées par l'API depuis la session, jamais lues dans la requête.
- Étant donné une requête qui tente de fixer l'auteur
  Quand elle est traitée
  Alors le champ est ignoré.
