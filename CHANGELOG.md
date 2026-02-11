# Changelog

Tous les changements notables dans DotnetNiger sont documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
et ce projet adhère à [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Structure complète du projet microservices
- Gateway API centralisée avec YARP
- Service d'identité avec JWT
- Service communautaire
- Documentation simplifiee (INDEX, SETUP, ARCHITECTURE, API)
- Identity: profil (update), change password, change email
- Identity: social links
- Identity: roles, permissions, admin endpoints
- Email provider configurable (SMTP, SendGrid, Mailgun)
- Seed admin configurable via variables d'environnement

### Changed

- Simplification et renommage des docs
- Mise a jour des liens README
- Ajout de la config Email et notes de seed admin

### Fixed

- Résolution des erreurs Swagger pour les schémas manquants

### Security

- Configuration JWT pour l'authentification
- Hachage des mots de passe en bcrypt
- Rate limiting par défaut

## [1.0.0] - 2026-01-29

### Added

#### Core Features

- [x] API Gateway avec YARP (Reverse Proxy)
  - Routage intelligent des requêtes
  - Load balancing
  - Agrégation Swagger

- [x] Identity Service
  - Authentification JWT
  - Gestion des utilisateurs
  - Refresh tokens
  - Profils utilisateur

- [x] Community Service
  - Gestion des posts
  - Système de commentaires
  - Likes/Dislikes
  - Feed utilisateur

#### Infrastructure

- [x] Docker & Docker Compose
- [x] SQL Server Database
- [x] Redis Cache
- [x] Prometheus Monitoring
- [x] Serilog Logging

#### API Features

- [x] Middleware d'authentification
- [x] Rate limiting
- [x] Error handling global
- [x] Request/Response logging
- [x] Cache management

#### Documentation

- [x] README.md
- [x] docs/SETUP.md
- [x] docs/ARCHITECTURE.md
- [x] docs/API.md
- [x] docs/INDEX.md
- [x] LICENSE (MIT)

### Security

- JWT token validation
- CORS configuration
- Password hashing
- Rate limiting
- Circuit breaker pattern

### Performance

- Response caching with Redis
- Database query optimization
- Connection pooling
- Async/await patterns

---

## Format des versions futures

### [X.Y.Z] - YYYY-MM-DD

#### Added

- Nouvelles fonctionnalités

#### Changed

- Changements dans les fonctionnalités existantes

#### Deprecated

- Fonctionnalités marquées pour suppression future

#### Removed

- Fonctionnalités supprimées

#### Fixed

- Bugs corrigés

#### Security

- Changements de sécurité

---

## Version Planning

### v1.1.0 (Prévu Q2 2026)

- [ ] Support OAuth2/OIDC
- [ ] Notifications en temps réel
- [ ] Système d'invitations
- [ ] Webhooks

### v1.2.0 (Prévu Q3 2026)

- [ ] API GraphQL
- [ ] Support multi-tenancy
- [ ] Dashboard d'administration
- [ ] Analytics avancées

### v2.0.0 (Prévu Q4 2026)

- [ ] Architecture event-driven
- [ ] CQRS pattern
- [ ] Machine learning recommendations
- [ ] Scalabilité horizontale complète

---

## Lignes directrices de versioning

### Règles de versioning

1. **MAJOR** (X.0.0) - Changements API incompatibles
2. **MINOR** (X.Y.0) - Nouvelles fonctionnalités compatibles
3. **PATCH** (X.Y.Z) - Corrections de bugs

### Exemples

- `1.0.0` → `1.0.1` : Correction de bug mineur
- `1.0.0` → `1.1.0` : Nouvelle fonctionnalité compatible
- `1.0.0` → `2.0.0` : Changement API breaking

---

## Notes de maintenance

### Support des versions

| Version | Status | Support until |
|---------|--------|----------------|
| 1.0.x | Stable | 2027-01-29 |
| 1.1.x | Beta | - |
| 2.0.x | Planned | - |

### Migration guide

Pour les mises à jour majeures, consultez les guides de migration spécifiques.

---

**Dernière mise à jour**: 29 Janvier 2026
