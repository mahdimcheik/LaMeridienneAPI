# LaMeridienneAPI

[![CI](https://github.com/mahdimcheik/LaMeridienneAPI/actions/workflows/ci.yml/badge.svg)](https://github.com/mahdimcheik/LaMeridienneAPI/actions/workflows/ci.yml)

API ASP.NET Core (`net10.0`) de La Meridienne : prise de notes avancée pour la boutique
(crédits clients, remboursements, ventes de journaux, notes du jour, rendez-vous pneus).
Client web : dépôt `LaMeridienneClient`.

La documentation fonctionnelle et technique est dans [`docs/`](docs/README.md) ; la liste des
tâches est la [feuille de route](docs/50-backlog/01-feuille-de-route.md).

```bash
./docs/check-docs.sh   # cohérence de la documentation
```

Développement : `cp .env.example .env` (configuration lue par l'API), puis voir
[exploitation](docs/30-specs-techniques/05-exploitation.md#développement-local).

Production (VPS derrière Nginx Proxy Manager) : voir
[exploitation](docs/30-specs-techniques/05-exploitation.md#production).

```bash
cp deploy/.env.example deploy/.env    # secrets, à remplir
docker compose -f deploy/compose.prod.yaml up -d --build
```
