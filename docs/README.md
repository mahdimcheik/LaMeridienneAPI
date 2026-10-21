# Spécifications LaMeridienneAPI

Source de vérité fonctionnelle et technique de La Meridienne. Tout désaccord entre le code et
ces documents est un bug — de l'un ou de l'autre — et doit être tranché explicitement.

La Meridienne est une **mini-application de prise de notes avancée** pour une boutique : crédits
accordés aux clients, remboursements, ventes de journaux, notes de la journée et rendez-vous
de montage de pneus. **Ce n'est pas un logiciel de comptabilité** : rien n'y est calculé,
rapproché ni déclaré.

Ces documents décrivent **la cible**. Au moment de leur rédaction, le dépôt ne contient que le
modèle de projet ASP.NET Core : toutes les user stories sont `Absente` et toutes les décisions
`À faire`. [La feuille de route](50-backlog/01-feuille-de-route.md) dit dans quel ordre on les
construit ; c'est aussi la liste des tâches.

Le client web vit dans le dépôt `LaMeridienneClient`, dont le dossier `docs/` reprend cette
structure pour ce qui est propre au front.

## Ordre de lecture

| # | Dossier | Contenu | Pour qui |
|---|---|---|---|
| 1 | [`00-vision/`](00-vision/) | Pourquoi le produit existe, pour qui, avec quel vocabulaire | tout le monde |
| 2 | [`10-user-stories/`](10-user-stories/) | Ce que chaque utilisateur peut faire, avec critères d'acceptation | produit, dev, QA |
| 3 | [`20-specs-fonctionnelles/`](20-specs-fonctionnelles/) | Les règles transverses et les permissions | produit, dev |
| 4 | [`30-specs-techniques/`](30-specs-techniques/) | Comment on le construit | dev, ops |
| 5 | [`40-decisions/`](40-decisions/) | Les arbitrages structurants et leurs conséquences (ADR) | dev |
| 6 | [`50-backlog/`](50-backlog/) | L'ordre de construction, ce qui reste à trancher, la dette | produit, dev |

**Raccourci si vous ne lisez qu'une chose :** [le glossaire](00-vision/03-glossaire.md),
puis [les règles métier](20-specs-fonctionnelles/01-regles-metier.md).

## Index

### 00 — Vision
- [Vision produit](00-vision/01-vision-produit.md)
- [Personas](00-vision/02-personas.md)
- [Glossaire](00-vision/03-glossaire.md)
- [Parcours utilisateurs](00-vision/04-parcours-utilisateurs.md)

### 10 — User stories
- [Conventions d'écriture](10-user-stories/README.md)
- [E01 — Authentification](10-user-stories/E01-authentification.md)
- [E02 — Feuille du jour](10-user-stories/E02-feuille-du-jour.md)
- [E03 — Crédits clients](10-user-stories/E03-credits-clients.md)
- [E04 — Remboursements](10-user-stories/E04-remboursements.md)
- [E05 — Ventes de journaux](10-user-stories/E05-ventes-journaux.md)
- [E06 — Notes générales](10-user-stories/E06-notes-generales.md)
- [E07 — Rendez-vous pneus](10-user-stories/E07-rendez-vous-pneus.md)
- [E08 — Administration](10-user-stories/E08-administration.md)
- [E09 — Transverse](10-user-stories/E09-transverse.md)

### 20 — Spécifications fonctionnelles
- [Règles métier](20-specs-fonctionnelles/01-regles-metier.md)
- [Rôles & permissions](20-specs-fonctionnelles/02-roles-permissions.md)

### 30 — Spécifications techniques
- [Architecture](30-specs-techniques/01-architecture.md)
- [Modèle de données](30-specs-techniques/02-modele-donnees.md)
- [Contrat d'API](30-specs-techniques/03-contrat-api.md)
- [Sécurité](30-specs-techniques/04-securite.md)
- [Exploitation](30-specs-techniques/05-exploitation.md)
- [Stratégie de tests](30-specs-techniques/06-strategie-tests.md)

### 40 — Décisions d'architecture
- [Index des ADR](40-decisions/README.md)

### 50 — Backlog
- [Feuille de route et tâches](50-backlog/01-feuille-de-route.md)
- [Questions ouvertes](50-backlog/02-questions-ouvertes.md)
- [Dette technique](50-backlog/03-dette-technique.md)

## Vérification

```bash
./docs/check-docs.sh
```

Contrôle les liens relatifs (y compris vers le code), les blocs de code, l'unicité des
identifiants de user story, la présence de leurs critères d'acceptation et de leur état,
le référencement des ADR, la validité des diagrammes mermaid, et que chaque contrôleur de
[`Controllers`](../Controllers/) est traité dans le
[contrat d'API](30-specs-techniques/03-contrat-api.md).

## Conventions

- **Langue** : français pour le fonctionnel, anglais pour les identifiants techniques
  (`DailySheet`, `CreditEntry`, `TireAppointment`) — ce sont des noms de code, on ne les
  traduit jamais dans le texte.
- **Montants** : en euros, tels que saisis, arrondis au centime. Aucun calcul de taxe.
- **Dates & heures** : instants stockés en UTC ; une **feuille du jour** est identifiée par une
  date calendaire locale (`Europe/Paris`), voir [ADR-0004](40-decisions/ADR-0004-feuille-par-jour.md).
- **Statut d'un document** : indiqué en tête (`Stable` · `À arbitrer` · `Brouillon`).
- **Référence croisée** : toujours un lien relatif vers le fichier, jamais une
  reformulation — une règle n'est écrite qu'à un seul endroit.
- **État d'implémentation** : chaque user story porte une ligne `État` —
  `Implémentée` · `Partielle` · `Absente` · `Hors API` (entièrement du ressort du front) —
  et chaque ADR une ligne `Application` — `Appliquée` · `Partielle` · `À faire`. Un écart est
  toujours décrit avec un lien vers le code concerné. Ces lignes sont mises à jour **dans la
  même contribution** que le code qui les change.
- **Liens vers le code** : vers le fichier, sans numéro de ligne — les numéros bougent à
  chaque modification, les noms de méthode restent.
