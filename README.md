# DotnetNiger

> Plateforme communautaire open-source pour la communauté DotnetNiger, construite avec .NET 8.0 LTS

[![.NET Version](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Status](https://img.shields.io/badge/Status-In%20Development-yellow.svg)](https://github.com/akaletekoffilevis/DotnetNiger)

## 📋 Vue d'ensemble

DotnetNiger est une plateforme communautaire moderne construite avec une architecture microservices .NET 8.0. Elle fournit des fonctionnalités de réseau social, forums de discussion et partage de contenu pour la communauté DotnetNiger.

> ⚠️ **Note:** Ce projet est actuellement en développement actif et n'est pas encore en production.

### Fonctionnalités Principales

- 🔐 **Authentication JWT** - Système d'authentification sécurisé avec tokens JWT
- 👥 **Gestion Utilisateurs** - Inscription, profils, rôles (Admin(Super Admin), Member)(Auto Attribut)
- 📝 **Posts & Commentaires** - Création et partage de contenu
- ❤️ **Système de Likes** - Interactions sociales
- 👤 **Follow/Unfollow** - Réseau social
- 🔍 **Recherche & Filtres** - Recherche avancée de contenu
- 🚀 **API Gateway** - Point d'entrée unique avec YARP
- 📊 **Monitoring** - Logs structurés et métriques
- 🐳 **Docker Ready** - Déploiement containerisé

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     API Gateway (5000)                  │
│                    YARP Reverse Proxy                   │
│        JWT Validation • Rate Limiting • CORS            │
└────────────────┬────────────────────────┬───────────────┘
                 │                        │
        ┌────────▼─────────┐     ┌───────▼──────────┐
        │  Identity (5075) │     │ Community (5269) │
        │  Authentication  │     │   Social Features│
        │  Authorization   │     │   Posts, Likes   │
        │  User Management │     │   Comments       │
        └──────────────────┘     └──────────────────┘
                 │                        │
        ┌────────▼────────────────────────▼────────────┐
        │           SQL Server 2022                    │
        │        (PostgreSQL 16+ supporté)             │
        └──────────────────────────────────────────────┘
                 │
        ┌────────▼────────┐
        │   Redis Cache   │
        └─────────────────┘
```

## 🚀 Quick Start

### Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server 2022](https://www.microsoft.com/sql-server/sql-server-downloads) ou [Docker](https://www.docker.com/products/docker-desktop)
- [Visual Studio Code](https://code.visualstudio.com/) (recommandé)

### Installation Rapide

```bash
# 1. Cloner le repository
git clone https://github.com/akaletekoffilevis/DotnetNiger.git
cd DotnetNiger

# 2. Restaurer les packages
dotnet restore

# 3. Configurer la base de données (SQL Server via Docker)ou utiliser le sqlite predefinit
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# 4. Appliquer les migrations
cd DotnetNiger.Identity
dotnet ef database update
cd ../DotnetNiger.Community
dotnet ef database update
cd ..

# 5. Lancer tous les services
.\run.ps1          # Windows
./run.sh           # Linux/Mac
```

### Tester l'API

```bash
# Inscription
curl -X POST http://localhost:5075/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123","firstName":"John","lastName":"Doe"}'

# Login
curl -X POST http://localhost:5075/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123"}'

## ⚙️ Configuration rapide (Identity)

- Email provider: voir la section Email dans [docs/API.md](docs/API.md)
```

## 📚 Documentation

| Document                             | Description                 |
| ------------------------------------ | --------------------------- |
| [Index](docs/INDEX.md)               | Navigation documentation    |
| [Setup](docs/SETUP.md)               | Installation et demarrage   |
| [Architecture](docs/ARCHITECTURE.md) | Vue d'ensemble des services |
| [API](docs/API.md)                   | Endpoints et auth           |
| [Changelog](CHANGELOG.md)            | Historique des versions     |
| [Security](SECURITY.md)              | Politique de securite       |
| [License](LICENSE.md)                | Licence MIT                 |

## 🛠️ Stack Technique

- **Framework:** .NET 8.0 LTS (C# 12)
- **API Gateway:** YARP (Yet Another Reverse Proxy)
- **Database:** SQL Server 2022 / PostgreSQL 16+ / Sqlite (Test Local)
- **ORM:** Entity Framework Core 8.0
- **Cache:** Redis
- **Authentication:** JWT Bearer
- **Logging:** Serilog
- **Monitoring:** Application Insights, Prometheus
- **Testing:** xUnit, Moq, FluentAssertions
- **Containers:** Docker, Docker Compose

## 🤝 Contribuer

Nous accueillons les contributions ! Voici comment commencer :

1. **Fork** le repository
2. **Clone** votre fork localement
3. **Créer une branche** depuis `dev` : `git checkout -b feat/ma-fonctionnalite`
4. **Commit** vos changements : `git commit -m "feat(scope): description"`
5. **Push** vers votre fork : `git push origin feat/ma-fonctionnalite`
6. **Créer une Pull Request** vers la branche `dev`

Ce guide suffit pour une premiere contribution.

### Workflow Git

- **`dev`** - Branche de développement (développez ici !)
- **`main`** - Branche de production (releases uniquement)

### Format des Commits

Nous utilisons [Conventional Commits](https://www.conventionalcommits.org/):

```bash
feat(community): add post search functionality
fix(identity): resolve token expiration bug
docs: update API documentation
```

## 📊 Statut du Projet

- ⏳ Architecture microservices (en cours)
- ⏳ API Gateway avec YARP (en cours)
- ⏳ Service Identity (Auth JWT) (en cours)
- ⏳ Service Community (Social) (en cours)
- ⏳ Docker & Docker Compose (en cours)
- ⏳ Documentation complète (en cours)
- ⏳ Tests unitaires (en cours)
- ⏳ Tests d'intégration (en cours)
- ⏳ CI/CD GitHub Actions (en cours)
- 📅 Déploiement production (prévu)
- 📅 Real-time features (prévu v1.1)

## 📄 Licence

Ce projet est sous licence MIT. Voir [LICENSE.md](LICENSE.md) pour les détails.

```
Copyright (c) 2026 DotnetNiger
```

## 👥 Auteur

- **Créateur & Mainteneur:** [@akaletekoffilevis](https://github.com/akaletekoffilevis)

> Ce projet est actuellement géré par son créateur en attendant sa mise en production et l'ouverture à la communauté.

## 🌐 Liens

- 📦 **Repository:** [github.com/akaletekoffilevis/DotnetNiger](https://github.com/akaletekoffilevis/DotnetNiger.git)
- 💬 **Discussions:** [GitHub Discussions](https://github.com/akaletekoffilevis/DotnetNiger/discussions)
- 📧 **Contact:** <abdallyacali@hotmail.com>

## 🎯 Roadmap

### Version 1.0.0 (En cours)

- [ ] Architecture microservices
- [ ] Services Identity & Community
- [ ] API Gateway
- [ ] Documentation complète
- [ ] Tests d'intégration complets
- [ ] CI/CD pipeline
- [ ] Déploiement initial

### Version 1.1.0 (Futur)

- [ ] Real-time notifications (SignalR)
- [ ] Advanced search (Elasticsearch)
- [ ] File upload service
- [ ] Email service (SendGrid)
- [ ] Admin dashboard

## ⭐ Support

Si vous trouvez ce projet intéressant ou utile :

- ⭐ Donnez une étoile sur GitHub
- 🐛 Signalez des bugs via les [Issues](https://github.com/akaletekoffilevis/DotnetNiger/issues)
- 💡 Proposez des features via les [Discussions](https://github.com/akaletekoffilevis/DotnetNiger/discussions)
- 🤝 Contribuez via des Pull Requests

---

**Made with ❤️ by the DotnetNiger Community**
