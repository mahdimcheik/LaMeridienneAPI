# E07 — Rendez-vous pneus

> Statut : **Stable** · Conventions : [README](README.md)

L'agenda des pneus est indépendant des feuilles du jour. Champs d'un rendez-vous : voir
[R-12](../20-specs-fonctionnelles/01-regles-metier.md#r-12--rendez-vous-pneus).

## US-E07-01 — Consulter l'agenda des pneus

**En tant que** monteur·se de pneus
**je veux** voir les rendez-vous de la semaine, ou d'une journée
**afin de** préparer l'atelier et savoir quand un pont est libre.

**Priorité :** Doit · **Dépend de :** US-E01-01

**État :** Absente

**Critères d'acceptation**

- Vue semaine par défaut, vue jour au choix ; navigation semaine/jour précédent et suivant,
  retour à aujourd'hui.
- Chaque rendez-vous affiche au moins l'heure, le client et le véhicule.
- Étant donné une période demandée de plus de 62 jours
  Quand l'agenda est chargé
  Alors la demande répond `400`.

## US-E07-02 — Ajouter un rendez-vous

**En tant qu'** employé·e au comptoir
**je veux** créer un rendez-vous en cliquant une plage libre de l'agenda
**afin de** réserver un créneau pendant que le client est au téléphone.

**Priorité :** Doit · **Dépend de :** US-E07-01, US-E03-04

**État :** Absente

**Critères d'acceptation**

- Le clic (ou la sélection au glisser) sur une plage ouvre une modale pré-remplie avec le début
  et la fin ; durée par défaut 1 h.
- Le client se choisit par autocomplétion ([US-E03-04](E03-credits-clients.md#us-e03-04--retrouver-un-client-par-autocomplétion)).
- Étant donné une fin antérieure ou égale au début
  Quand je valide
  Alors le rendez-vous est refusé avec un message explicite.

- Étant donné un créneau qui chevauche un autre rendez-vous
  Quand je valide
  Alors le rendez-vous est créé **et** un avertissement nomme le rendez-vous en conflit
  ([R-13](../20-specs-fonctionnelles/01-regles-metier.md#r-13--chevauchement-de-rendez-vous)).

- Un rendez-vous dans le passé est accepté (saisie a posteriori).

## US-E07-03 — Consulter et modifier un rendez-vous

**En tant qu'** utilisateur
**je veux** ouvrir un rendez-vous dans une modale pour le lire ou le modifier
**afin de** retrouver le téléphone du client ou corriger le véhicule.

**Priorité :** Doit · **Dépend de :** US-E07-02

**État :** Absente

**Critères d'acceptation**

- Un clic sur le rendez-vous ouvre la modale en **consultation** ; un bouton passe en édition.
- Mêmes contrôles qu'à la création.
- La modale se ferme à Échap et rend le focus au rendez-vous ouvert.
- Étant donné un rendez-vous supprimé entre-temps
  Quand j'enregistre
  Alors la demande répond `404` et l'agenda est rechargé.

## US-E07-04 — Déplacer un rendez-vous par glisser-déposer

**En tant que** monteur·se de pneus
**je veux** faire glisser un rendez-vous vers une autre plage, ou étirer sa fin
**afin de** décaler un client en retard sans rouvrir la fiche.

**Priorité :** Doit · **Dépend de :** US-E07-02

**État :** Absente

**Critères d'acceptation**

- Le déplacement conserve la durée ; l'étirement change la fin seule.
- Le pas de déplacement est de 15 minutes.
- Étant donné un déplacement refusé par l'API
  Quand la réponse arrive
  Alors le rendez-vous revient à sa place d'origine avec un message.

- Le déplacement est aussi possible au clavier depuis la modale d'édition (champs date/heure),
  pour qui n'utilise pas de souris.

## US-E07-05 — Supprimer un rendez-vous

**En tant qu'** utilisateur
**je veux** supprimer un rendez-vous annulé
**afin de** libérer le créneau.

**Priorité :** Doit · **Dépend de :** US-E07-02

**État :** Absente

**Critères d'acceptation**

- Depuis la modale, après confirmation.
- Suppression logique ([R-06](../20-specs-fonctionnelles/01-regles-metier.md#r-06--suppression-logique)).
- Étant donné un rendez-vous déjà supprimé
  Quand je le supprime
  Alors la demande répond `404`.
