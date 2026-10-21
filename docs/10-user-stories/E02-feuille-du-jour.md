# E02 — Feuille du jour

> Statut : **Stable** · Conventions : [README](README.md) ·
> Décision : [ADR-0004](../40-decisions/ADR-0004-feuille-par-jour.md)

La feuille du jour est une page A4 paysage découpée en quatre cases : crédits clients,
remboursements, journaux vendus, notes générales.

## US-E02-01 — Ouvrir la feuille du jour

**En tant qu'** utilisateur
**je veux** arriver directement sur la feuille d'aujourd'hui
**afin de** commencer à noter sans chercher ni créer quoi que ce soit.

**Priorité :** Doit · **Dépend de :** US-E01-01

**État :** Implémentée · **Code :** [`SheetsController`](../../Controllers/SheetsController.cs)

**Critères d'acceptation**

- Étant donné qu'aucune feuille n'existe pour aujourd'hui
  Quand je l'ouvre
  Alors une feuille vide est créée et affichée, avec les quatre cases.

- Étant donné que la feuille d'aujourd'hui existe
  Quand je l'ouvre, depuis n'importe quel poste
  Alors je vois les saisies de tous les utilisateurs.

- « Aujourd'hui » est la date locale de la boutique
  ([R-01](../20-specs-fonctionnelles/01-regles-metier.md#r-01--date-dune-feuille)).

- Deux ouvertures simultanées de la même date ne créent jamais deux feuilles.

## US-E02-02 — Consulter une autre journée

**En tant qu'** utilisateur
**je veux** passer au jour précédent ou suivant, ou choisir une date
**afin de** retrouver ce qui a été noté un autre jour.

**Priorité :** Doit · **Dépend de :** US-E02-01

**État :** Implémentée · **Code :** [`SheetsController`](../../Controllers/SheetsController.cs)

**Critères d'acceptation**

- Étant donné une date passée sans feuille
  Quand je l'ouvre
  Alors une feuille vide s'affiche, **sans** être enregistrée tant que rien n'y est saisi.

- Étant donné une date passée avec une feuille
  Quand je l'ouvre
  Alors ses lignes sont affichées et modifiables comme celles du jour.

- Étant donné une date mal formée
  Quand elle est demandée
  Alors la demande répond `400`.

- La date affichée est toujours visible en tête de feuille, au format long
  (« mardi 21 octobre 2026 »).

## US-E02-03 — Ajuster la taille des cases

**En tant qu'** utilisateur
**je veux** agrandir ou réduire les cases de la feuille
**afin de** donner de la place à la case qui se remplit le plus ce jour-là.

**Priorité :** Devrait · **Dépend de :** US-E02-01

**État :** Implémentée · **Code :** [`AccountController`](../../Controllers/AccountController.cs)

**Critères d'acceptation**

- Étant donné la feuille affichée
  Quand je fais glisser la bordure entre deux cases
  Alors les deux cases changent de taille et la feuille reste au format A4 paysage.

- Une case ne peut pas descendre sous une taille minimale qui laisse lisible son titre et
  une ligne.

- Étant donné une disposition modifiée
  Quand je me reconnecte, sur n'importe quel poste
  Alors je retrouve ma disposition ([R-07](../20-specs-fonctionnelles/01-regles-metier.md#r-07--disposition-des-cases)).

- Étant donné une disposition enregistrée invalide (cases hors feuille, taille nulle)
  Quand elle est envoyée
  Alors elle est refusée et la disposition par défaut reste en place.

- Un bouton « Disposition par défaut » rétablit le découpage initial.

## US-E02-04 — Imprimer la feuille

**En tant que** gérant·e
**je veux** imprimer la feuille d'un jour sur une page A4 paysage
**afin d'** en garder une trace papier ou la laisser au collègue sans poste.

**Priorité :** Pourrait · **Dépend de :** US-E02-01

**État :** Hors API

**Critères d'acceptation**

- L'impression tient sur **une** page A4 paysage, sans la navigation de l'application.
- Toutes les lignes sont imprimées, même celles masquées par le défilement d'une case.
- Étant donné une case qui déborde d'une page
  Quand j'imprime
  Alors un message prévient que la feuille sera imprimée sur plusieurs pages.
