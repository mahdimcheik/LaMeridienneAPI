# Stratégie de tests

> Statut : **Stable**

| Niveau | Outil | Cible |
|---|---|---|
| Unitaire | xUnit | services : règles `R-xx` (montants, verrouillage, rotation, incréments, chevauchement) |
| Intégration | xUnit + `WebApplicationFactory` + Testcontainers PostgreSQL | chaque contrôleur : nominal, `400`, `401`, `404`, `409` |
| Mise en service | xUnit + Testcontainers ; scripts `deploy/` à la main | migration au démarrage sur base absente, limitation de débit derrière le proxy ; restauration répétée par `deploy/check-restore.sh` |

Projet `tests/LaMeridienne.Tests`, lancé par `dotnet test LaMeridienne.slnx` et par la CI à chaque *push*. Un test cite l'identifiant de la story ou
de la règle qu'il vérifie dans son nom (`US_E03_01_refuse_un_montant_negatif`).

Priorités de couverture :

1. Authentification : rotation, réutilisation, verrouillage, `401` partout ailleurs.
2. Unicité d'une feuille par date sous accès concurrent.
3. Incréments concurrents des journaux ([R-09](../20-specs-fonctionnelles/01-regles-metier.md#r-09--incréments-concurrents)).
4. Conflit de version des notes ([R-11](../20-specs-fonctionnelles/01-regles-metier.md#r-11--édition-concurrente)).

`TimeProvider` est remplacé par une horloge figée pour tester le passage de minuit à Paris.
