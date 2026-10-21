# Sécurité

> Statut : **Stable** · Décisions : [ADR-0002](../40-decisions/ADR-0002-jwt-et-refresh-token.md),
> [ADR-0003](../40-decisions/ADR-0003-comptes-pre-crees.md)

## Session

| Élément | Valeur |
|---|---|
| Jeton d'accès | JWT HS256, 15 min, claims `sub`, `name`, `role=Admin`, `jti`, `sid` (jeton de renouvellement de la session) ; renvoyé dans le corps |
| Jeton de renouvellement | 32 octets aléatoires, 30 jours glissants, **haché (SHA-256)** en base |
| Cookie de renouvellement | `lm_refresh`, `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth` |
| Rotation | à chaque `refresh` ; réutilisation ⇒ révocation de toutes les sessions ([R-03](../20-specs-fonctionnelles/01-regles-metier.md#r-03--réutilisation-dun-jeton-de-renouvellement)) |
| Verrouillage | [R-02](../20-specs-fonctionnelles/01-regles-metier.md#r-02--verrouillage) ; identifiant inconnu et mauvais mot de passe : même réponse `401`, même coût de hachage |
| Déconnexion | par le seul cookie, sans jeton d'accès : le détenir suffit pour le révoquer, et elle reste possible quand le jeton d'accès a expiré |
| Changement de mot de passe | révoque toutes les sessions du compte sauf celle du `sid` courant |

Code : [`AuthSetup`](../../Auth/AuthSetup.cs), [`TokenService`](../../Auth/TokenService.cs),
[`AuthService`](../../Auth/AuthService.cs).

`SameSite=Strict` et `Path=/api/auth` limitent le cookie aux seuls appels d'authentification,
ce qui suffit contre la falsification de requête : les autres appels n'utilisent que l'en-tête
`Authorization`.

## Secrets et configuration

Lus depuis les variables d'environnement (ou `dotnet user-secrets` en développement), **jamais**
commités. L'API refuse de démarrer si l'un manque.

| Variable | Rôle |
|---|---|
| `ConnectionStrings__Default` | chaîne PostgreSQL |
| `Jwt__Key` | clé de signature, ≥ 32 octets |
| `Jwt__Issuer`, `Jwt__Audience` | émetteur et audience |
| `Seed__Users__0__UserName` … `Seed__Users__2__Password` | les trois comptes initiaux ; mot de passe ≥ 12 caractères ([R-14](../20-specs-fonctionnelles/01-regles-metier.md#r-14--mot-de-passe)) |
| `Seed__Users__n__DisplayName` | nom affiché (facultatif, l'identifiant par défaut) |
| `Cors__Origins` | origines autorisées (dev) |

## Autres mesures

- HTTPS obligatoire hors développement : TLS terminé par le reverse proxy, HSTS posé par
  l'API (`UseHsts`) hors développement.
- Derrière le proxy, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` : l'adresse du client et le
  schéma viennent de `X-Forwarded-For` / `X-Forwarded-Proto`. Ces en-têtes sont crus de toute
  source, donc l'API ne doit être joignable que par le proxy (aucun port publié,
  [exploitation](05-exploitation.md#production)).
- Limitation de débit sur `/api/auth/login` et `/api/auth/refresh` (`AddRateLimiter`,
  10 requêtes/minute/IP, `429` avec `Retry-After`).
- Aucun HTML n'est stocké ni rendu ; les textes libres sont rendus en texte par le front.
- Les identifiants d'auteur sont pris dans la session, jamais dans la requête.
