# Contrat d'API

> Statut : **Brouillon** — la référence exécutable sera le document OpenAPI généré.

Préfixe `/api`. JSON en `camelCase`, instants en UTC avec `Z`, dates de feuille `AAAA-MM-JJ`. Toutes les routes exigent un jeton d'accès sauf mention
contraire. Erreurs au format `ProblemDetails` (`application/problem+json`), message `title`/
`detail` en français, `errors` par champ pour une validation (`400`).

| Code | Usage |
|---|---|
| `400` | entrée invalide |
| `401` | jeton absent, expiré ou révoqué |
| `404` | ressource inconnue ou supprimée |
| `409` | conflit de version (notes) ou de nom (journal, client) |
| `423` | compte verrouillé |
| `429` | trop de requêtes sur `login`/`refresh` (`Retry-After`) |

## Contrôleurs

| Contrôleur | Route | Stories |
|---|---|---|
| `AuthController` | `POST /api/auth/login` *(anonyme)* · `POST /api/auth/refresh` *(anonyme, cookie)* · `POST /api/auth/logout` *(anonyme, cookie)* · `GET /api/auth/me` | E01 |
| `AccountController` | `PUT /api/account/password` · `GET`/`PUT /api/account/layout` | US-E08-03, US-E02-03 |
| `SheetsController` | `GET /api/sheets/today` · `GET /api/sheets/{date}` · `PUT /api/sheets/{date}/notes` | E02, E06 |
| `CreditsController` | `POST /api/sheets/{date}/credits` · `PUT`/`DELETE /api/credits/{id}` | E03 |
| `RefundsController` | `POST /api/sheets/{date}/refunds` · `PUT`/`DELETE /api/refunds/{id}` | E04 |
| `NewspaperSalesController` | `PUT /api/sheets/{date}/newspapers/{newspaperId}` (valeur) · `POST …/increment` (`delta` ±1) | E05 |
| `NewspapersController` | `GET`/`POST /api/newspapers` · `PUT /api/newspapers/{id}` · `PUT /api/newspapers/order` | US-E08-01 |
| `CustomersController` | `GET /api/customers?search=&limit=` · `GET /api/customers/{id}/credits` · `PUT /api/customers/{id}` · `POST /api/customers/{id}/merge` | US-E03-04, US-E03-05, US-E08-02 |
| `AppointmentsController` | `GET /api/appointments?from=&to=` · `POST` · `PUT`/`DELETE /api/appointments/{id}` | E07 |
| *(aucun)* | `GET /health` *(anonyme)* — `200 Healthy` si la base répond, `503` sinon | — |

## Formes principales

```jsonc
// GET /api/sheets/2026-10-21 — date sans feuille : "exists": false, listes vides, version 0
// GET /api/sheets/today — crée la feuille du jour si besoin (R-01)
{
  "date": "2026-10-21",
  "exists": true,
  "notes": "Livraison Sud Ouest en retard",
  "notesVersion": 4,
  "credits":  [{ "id": "…", "date": "2026-10-21", "customer": { "id": "…", "name": "Dupont" }, "amount": 12.5,
                 "comment": null, "createdBy": "Marie", "createdAt": "2026-10-21T07:12:00Z",
                 "updatedBy": null, "updatedAt": null }],
  "refunds":  [],
  "newspapers": [{ "newspaperId": "…", "name": "Sud Ouest", "isActive": true, "quantity": 14 }] // lot 3
}
```

```jsonc
// POST /api/sheets/2026-10-21/credits — client par id OU par nom (créé à la volée, R-08)
// → 201 avec la ligne ; même corps pour PUT /api/credits/{id} (200) et pour les remboursements
{ "customerId": null, "customerName": "Dupont", "amount": 12.5, "comment": "pain + journal" }
```

```jsonc
// PUT /api/sheets/2026-10-21/notes → 200 { "notes", "notesVersion" } ; crée la feuille si besoin
{ "notes": "Livraison Sud Ouest en retard", "version": 3 }
// version périmée → 409, avec l'état serveur pour que le front le présente (R-11)
{ "title": "Conflit", "status": 409, "detail": "…", "notes": "texte serveur", "notesVersion": 4 }
```

```jsonc
// GET /api/customers?search=dup&limit=10 → au plus 10, « contient », sans casse ni accents (≥ 2 caractères)
[{ "id": "…", "name": "Dupont", "phone": null }]
// GET /api/customers/{id}/credits → du plus récent au plus ancien, aucun total (R-04)
{ "customer": { "id": "…", "name": "Dupont", "phone": null }, "credits": [ /* lignes, avec "date" */ ] }
```

```jsonc
// GET /api/account/layout — "isDefault": true tant que rien n'est enregistré ; PUT : même forme
{ "topSplit": 0.5, "bottomSplit": 0.5, "rowSplit": 0.5, "isDefault": true }
```

```jsonc
// POST /api/appointments → 201, avec les conflits éventuels (R-13)
{ "appointment": { "id": "…", "start": "…", "end": "…", "customer": { … }, "vehicle": "Clio AB-123-CD" },
  "overlaps": [{ "id": "…", "start": "…", "end": "…", "customerName": "Martin" }] }
```

Le jeton d'accès est renvoyé dans le corps de `login`/`refresh` ; le jeton de renouvellement
**uniquement** en cookie ([sécurité](04-securite.md)).

```jsonc
// POST /api/auth/login → 200 (401 identifiants refusés, 423 verrouillé, 429 trop de requêtes)
{ "userName": "marie", "password": "…" }
// réponse de login et refresh ; GET /api/auth/me rend seulement l'objet "user"
{ "accessToken": "eyJ…", "expiresAt": "2026-10-21T08:15:00Z",
  "user": { "id": "…", "userName": "marie", "displayName": "Marie" } }
```

```jsonc
// PUT /api/account/password → 204 ; 400 avec errors.currentPassword ou errors.newPassword
{ "currentPassword": "…", "newPassword": "au moins 12 caractères" }
```

`refresh` et `logout` ne lisent que le cookie `lm_refresh` ; `logout` rend `204` même sans
session.
