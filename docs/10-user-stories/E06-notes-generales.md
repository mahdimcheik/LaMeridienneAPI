# E06 — Notes générales

> Statut : **Stable** · Conventions : [README](README.md)

## US-E06-01 — Écrire les notes de la journée

**En tant qu'** utilisateur
**je veux** écrire du texte libre dans la case Notes de la feuille
**afin de** transmettre une information au collègue suivant sans papier volant.

**Priorité :** Doit · **Dépend de :** US-E02-01

**État :** Implémentée · **Code :** [`SheetsController`](../../Controllers/SheetsController.cs)

**Critères d'acceptation**

- Texte brut multiligne, sans mise en forme ; les retours à la ligne sont conservés.
- L'enregistrement est automatique ([US-E09-02](E09-transverse.md#us-e09-02--ne-jamais-perdre-une-saisie)).
- Étant donné un texte au-delà de 10 000 caractères
  Quand il est enregistré
  Alors il est refusé et le compteur de caractères le signale avant envoi.

- Étant donné deux postes qui modifient les notes du même jour
  Quand le second enregistre
  Alors l'enregistrement le plus récent l'emporte et le premier poste est prévenu que le
  texte a changé ([R-11](../20-specs-fonctionnelles/01-regles-metier.md#r-11--édition-concurrente)).

- Le texte est affiché tel quel : aucun HTML saisi n'est interprété.
