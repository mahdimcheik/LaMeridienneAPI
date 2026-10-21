# Questions ouvertes

> Statut : **À arbitrer** — chaque question porte une **proposition par défaut**. Tant qu'elle
> n'est pas contestée, c'est elle qui s'applique et qui est écrite dans les specs.

## Q1 — Chevauchement de rendez-vous pneus

**Contexte.** L'atelier peut avoir plusieurs ponts, ou un seul.

| Option | Conséquence |
|---|---|
| Refuser tout chevauchement | sûr si un seul pont, bloquant sinon |
| **Accepter avec avertissement** *(proposé)* | souple, l'utilisateur décide |
| Nombre de ponts configurable | exact, mais un réglage de plus |

> **Proposition :** accepter avec avertissement ([R-13](../20-specs-fonctionnelles/01-regles-metier.md#r-13--chevauchement-de-rendez-vous)).

## Q2 — Champs d'un rendez-vous pneus

> **Proposition :** client, téléphone, véhicule/immatriculation, prestation, note
> ([R-12](../20-specs-fonctionnelles/01-regles-metier.md#r-12--rendez-vous-pneus)). À compléter
> si l'atelier a besoin de la dimension des pneus ou d'un statut (« pneus commandés »).

## Q3 — Disposition des cases : par utilisateur ou partagée ?

| Option | Conséquence |
|---|---|
| **Par utilisateur** *(proposé)* | chacun garde ses préférences ; l'impression peut différer selon qui imprime |
| Partagée | la même feuille pour tous ; un collègue peut défaire votre réglage |
| Par feuille | chaque jour peut avoir sa disposition ; plus de données, peu d'intérêt |

> **Proposition :** par utilisateur ([R-07](../20-specs-fonctionnelles/01-regles-metier.md#r-07--disposition-des-cases)).

## Q4 — Édition simultanée à deux postes

> **Proposition :** dernier enregistrement gagnant, sauf pour les notes générales où un conflit
> de version est signalé ([R-11](../20-specs-fonctionnelles/01-regles-metier.md#r-11--édition-concurrente)).
> Pas de temps réel dans la première version : la feuille se recharge à l'ouverture et à la
> reprise du focus de l'onglet.

## Q5 — Prix des journaux

> **Proposition :** non géré, quantités seulement. Ajouter un prix ferait glisser l'application
> vers la comptabilité, hors [périmètre](../00-vision/01-vision-produit.md).

## Q6 — Durée des sessions

> **Proposition :** accès 15 minutes, renouvellement 30 jours glissants — un poste de comptoir
> reste connecté tant qu'il sert au moins une fois par mois
> ([R-03](../20-specs-fonctionnelles/01-regles-metier.md#r-03--réutilisation-dun-jeton-de-renouvellement)).

## Q7 — Suppression d'un crédit réglé

**Contexte.** Quand un client règle son crédit, on peut supprimer la ligne ou la marquer réglée.

> **Proposition :** supprimer la ligne (comme on raye sur le papier). Si l'historique des
> règlements devient utile, ajouter un statut « réglé » dans un second temps.
