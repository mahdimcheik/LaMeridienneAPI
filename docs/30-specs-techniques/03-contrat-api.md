# Contrat d'API

> Statut : **Brouillon** — la référence exécutable sera le document OpenAPI généré.

Préfixe `/api`. JSON en `camelCase`. Toutes les routes exigent un jeton d'accès sauf mention
contraire. Erreurs au format `ProblemDetails` (`application/problem+json`), message `title`/
`detail` en français, `errors` par champ pour une validation (`400`).

| Code | Usage |
|---|---|
| `400` | entrée invalide |
| `401` | jeton absent, expiré ou révoqué |
| `404` | ressource inconnue ou supprimée |
| `409` | conflit de version (notes) ou de nom (journal, client) |
| `423` | compte verrouillé |

## Contrôleurs

| Contrôleur | Route | Stories |
|---|---|---|
| `AuthController` | `POST /api/auth/login` *(anonyme)* · `POST /api/auth/refresh` *(anonyme, cookie)* · `POST /api/auth/logout` · `GET /api/auth/me` | E01 |
| `AccountController` | `PUT /api/account/password` · `GET`/`PUT /api/account/layout` | US-E08-03, US-E02-03 |
| `SheetsController` | `GET /api/sheets/today` · `GET /api/sheets/{date}` · `PUT /api/sheets/{date}/notes` | E02, E06 |
| `CreditsController` | `POST /api/sheets/{date}/credits` · `PUT`/`DELETE /api/credits/{id}` | E03 |
| `RefundsController` | `POST /api/sheets/{date}/refunds` · `PUT`/`DELETE /api/refunds/{id}` | E04 |
| `NewspaperSalesController` | `PUT /api/sheets/{date}/newspapers/{newspaperId}` (valeur) · `POST …/increment` (`delta` ±1) | E05 |
| `NewspapersController` | `GET`/`POST /api/newspapers` · `PUT /api/newspapers/{id}` · `PUT /api/newspapers/order` | US-E08-01 |
| `CustomersController` | `GET /api/customers?search=&limit=` · `GET /api/customers/{id}/credits` · `PUT /api/customers/{id}` · `POST /api/customers/{id}/merge` | US-E03-04, US-E03-05, US-E08-02 |
| `AppointmentsController` | `GET /api/appointments?from=&to=` · `POST` · `PUT`/`DELETE /api/appointments/{id}` | E07 |
| `WeatherForecastController` | **à supprimer** — reliquat du modèle de projet (lot 0 de la [feuille de route](../50-backlog/01-feuille-de-route.md)) | — |

## Formes principales

```jsonc
// GET /api/sheets/2026-10-21
{
  "date": "2026-10-21",
  "exists": true,
  "notes": "Livraison Sud Ouest en retard",
  "notesVersion": 4,
  "credits":  [{ "id": "…", "customer": { "id": "…", "name": "Dupont" }, "amount": 12.5, "comment": null,
                 "createdBy": "Marie", "createdAt": "2026-10-21T07:12:00Z", "updatedBy": null, "updatedAt": null }],
  "refunds":  [],
  "newspapers": [{ "newspaperId": "…", "name": "Sud Ouest", "isActive": true, "quantity": 14 }]
}
```

```jsonc
// POST /api/sheets/2026-10-21/credits — client par id OU par nom (créé à la volée, R-08)
{ "customerId": null, "customerName": "Dupont", "amount": 12.5, "comment": "pain + journal" }
```

```jsonc
// POST /api/appointments → 201, avec les conflits éventuels (R-13)
{ "appointment": { "id": "…", "start": "…", "end": "…", "customer": { … }, "vehicle": "Clio AB-123-CD" },
  "overlaps": [{ "id": "…", "start": "…", "end": "…", "customerName": "Martin" }] }
```

Le jeton d'accès est renvoyé dans le corps de `login`/`refresh` (`{ "accessToken", "expiresAt",
"user" }`) ; le jeton de renouvellement **uniquement** en cookie ([sécurité](04-securite.md)).
