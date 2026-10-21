# Règles métier

> Statut : **Stable** — chaque règle n'est écrite qu'ici ; les stories et les specs y renvoient
> par leur identifiant `R-xx`, jamais par une reformulation. Les valeurs marquées *(Qn)* sont la
> proposition par défaut d'une [question ouverte](../50-backlog/02-questions-ouvertes.md).

## R-01 — Date d'une feuille

Une feuille du jour est identifiée par une **date calendaire** (`DateOnly`), dans le fuseau de
la boutique, `Europe/Paris`. « Aujourd'hui » est calculé par l'API dans ce fuseau, pas par le
navigateur. Une date ne porte **qu'une** feuille. Voir
[ADR-0004](../40-decisions/ADR-0004-feuille-par-jour.md).

Une feuille n'est enregistrée qu'à sa première saisie ; consulter une date vide ne crée rien,
sauf pour aujourd'hui (la feuille du jour est créée à l'ouverture).

## R-02 — Verrouillage

Après **5** échecs de connexion consécutifs, un compte est verrouillé **15 minutes**. Une
connexion réussie remet le compteur à zéro. Le message ne révèle pas si l'identifiant existe.

## R-03 — Réutilisation d'un jeton de renouvellement

Un jeton de renouvellement est à **usage unique**. Présenter un jeton déjà consommé est traité
comme un vol : **toutes** les sessions du compte sont révoquées. Durées : accès 15 minutes,
renouvellement 30 jours glissants *(Q6)*.

## R-04 — Crédits et remboursements sont indépendants

Un crédit et un remboursement sont deux notes sans lien. L'application **ne calcule aucun
solde**, n'associe pas un remboursement à un crédit et ne présente aucun total comme une somme
due. Elle peut afficher un total **par case et par jour**, libellé comme tel (« Total noté ce
jour »).

## R-05 — Montants

Montant en euros, strictement positif, au plus **10 000 €**, deux décimales au plus. La
virgule et le point sont acceptés à la saisie. Stockage en `numeric(10,2)`.

## R-06 — Suppression logique

Les lignes de crédit, de remboursement et les rendez-vous ne sont pas effacés : ils portent une
date de suppression et disparaissent de toutes les lectures. Aucun écran ne les restaure dans
la première version.

## R-07 — Disposition des cases

La disposition (taille relative des quatre cases) est **propre à chaque utilisateur** *(Q3)* et
s'applique à toutes les feuilles. Elle est exprimée en proportions de la feuille (0–1), pas en
pixels, pour rester valable quelle que soit la taille d'écran. Chaque case occupe au moins 15 %
de la largeur et 15 % de la hauteur. Sans disposition enregistrée, la disposition par défaut
s'applique : crédits et remboursements en haut, journaux et notes en bas, quatre quarts égaux.

## R-08 — Référentiel clients

Un client n'a qu'un nom (obligatoire, 1 à 100 caractères, espaces superflus retirés) et un
téléphone facultatif. Deux clients ne peuvent pas porter le même nom, comparé sans casse ni
accents. Saisir un nom inconnu crée le client ; saisir un nom connu réutilise la fiche.

## R-09 — Incréments concurrents

Les boutons `+1`/`−1` des journaux envoient un **incrément**, pas une valeur absolue, pour que
deux postes ne s'écrasent pas. La saisie directe d'une quantité envoie une valeur absolue. La
quantité ne descend jamais sous 0 ; un `−1` à 0 est ignoré.

## R-10 — Journal désactivé

Un journal n'est jamais supprimé. Désactivé, il n'apparaît plus sur une feuille **sauf** si
celle-ci porte une vente de ce journal (quantité supérieure à 0). Sud Ouest et Le Républicain sont créés au premier
démarrage, actifs, dans cet ordre.

## R-11 — Édition concurrente

Sur une même donnée modifiée depuis deux postes, **le dernier enregistrement l'emporte** *(Q4)*.
Pour les notes générales, l'API rejette avec `409` une modification fondée sur une version
périmée ; le front affiche alors le texte serveur et propose de recopier le sien.

## R-12 — Rendez-vous pneus

Champs *(Q2)* : début, fin (obligatoires), client (obligatoire, référentiel de R-08), téléphone,
véhicule/immatriculation, prestation (texte libre, ex. « 4 pneus été + équilibrage »), note.
Durée entre 15 minutes et 8 heures, bornes alignées sur un pas de 15 minutes.

## R-13 — Chevauchement de rendez-vous

Deux rendez-vous peuvent se chevaucher (plusieurs ponts) *(Q1)*. L'API accepte et renvoie la
liste des rendez-vous en conflit ; le front affiche un avertissement.

## R-14 — Mot de passe

Au moins 12 caractères. Aucune autre règle de composition. Haché par l'algorithme d'ASP.NET
Core Identity (`PasswordHasher`).
