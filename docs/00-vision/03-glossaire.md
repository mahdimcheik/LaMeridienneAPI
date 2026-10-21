# Glossaire

> Statut : **Stable**

| Terme | Code | Définition |
|---|---|---|
| Feuille du jour | `DailySheet` | Page unique d'une date calendaire, au format A4 paysage, qui regroupe les cases de la journée. Une seule par date. |
| Case | `SheetSection` | Zone de la feuille dédiée à un type de saisie : crédits, remboursements, journaux, notes. Redimensionnable. |
| Disposition | `SheetLayout` | Taille et position des cases, propres à chaque utilisateur. |
| Client | `Customer` | Nom d'une personne cliente, proposé par autocomplétion. Pas de coordonnées obligatoires. |
| Crédit client | `CreditEntry` | Ligne notant qu'un client a emporté sans payer : client, montant, commentaire. |
| Remboursement | `RefundEntry` | Ligne notant qu'une somme a été rendue à un client. **Indépendant** des crédits : aucun lien, aucun solde. |
| Journal | `Newspaper` | Titre de presse vendu en boutique (Sud Ouest, Le Républicain…). Actif ou désactivé. |
| Vente de journaux | `NewspaperSale` | Quantité vendue d'un journal sur une feuille du jour. |
| Notes générales | `DailySheet.Notes` | Texte libre de la journée. |
| Rendez-vous pneus | `TireAppointment` | Créneau de l'agenda des pneus : début, fin, client, véhicule, prestation, note. |
| Utilisateur | `User` | L'un des trois comptes pré-créés. Tous administrateurs. |
| Jeton d'accès | — | Jeton JWT de courte durée envoyé à chaque appel. |
| Jeton de renouvellement | `RefreshToken` | Jeton de longue durée, à usage unique, qui permet d'obtenir un nouveau jeton d'accès. |
