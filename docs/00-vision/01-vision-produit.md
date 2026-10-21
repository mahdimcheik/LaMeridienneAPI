# Vision produit

> Statut : **Stable**

## Le problème

À la boutique, tout ce qui sort de la caisse se note aujourd'hui sur papier : le client
habitué qui paiera demain, l'avoir rendu à un autre, le nombre de journaux vendus, un message
pour le collègue de l'après-midi, le rendez-vous de montage de pneus pris au téléphone. Le
papier se perd, ne se relit pas à deux postes, et ne permet pas de retrouver « ce que M. Dupont
doit depuis mardi ».

## La réponse

Une **feuille du jour numérique**, qui ressemble volontairement à la feuille A4 posée à côté
de la caisse — même format paysage, mêmes cases — mais qui se sauvegarde seule, se retrouve
par date et propose les noms de clients déjà saisis. À côté, un **agenda des rendez-vous
pneus** remplace le cahier de rendez-vous.

## Périmètre

| Dans le périmètre | Hors périmètre |
|---|---|
| Noter un crédit client, un remboursement | Calculer un solde, relancer un client |
| Compter les journaux vendus par titre | Gérer un stock, des livraisons, des invendus |
| Laisser des notes libres sur la journée | Messagerie, notifications |
| Planifier des rendez-vous pneus | Devis, facturation, stock de pneus |
| 3 comptes fixes | Inscription, gestion fine des droits |

**Ce n'est pas un logiciel de comptabilité.** Aucun chiffre saisi n'a de valeur comptable ni
fiscale ; il n'y a ni total légal, ni clôture, ni export comptable. Toute demande qui y
ressemble est hors périmètre et doit passer par une
[question ouverte](../50-backlog/02-questions-ouvertes.md).

## Principes

1. **Aussi rapide que le papier.** Une saisie courante (un crédit, une quantité de journaux)
   se fait sans quitter la feuille, en quelques secondes, sans bouton « Enregistrer ».
2. **Rien ne se perd.** Chaque saisie est enregistrée côté serveur dès qu'elle est validée et
   reste consultable sur la feuille de son jour.
3. **Simple à exploiter.** Une API, une base PostgreSQL, pas de service tiers.

## Indicateurs de réussite

- Le carnet papier n'est plus utilisé après un mois.
- Retrouver une saisie d'un jour passé prend moins de 30 secondes.
