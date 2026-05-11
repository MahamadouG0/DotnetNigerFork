# Setup Local

## Prerequis

- .NET SDK 8.x
- Node.js + npm (pour outils frontend/scripts)
- Git

## Clonage et restauration

```bash
git clone https://github.com/DelaliAbel/DotnetNiger.git
cd DotnetNiger
dotnet restore DotnetNiger.slnx
```

## Ordre de demarrage recommande

1. Identity
2. Community
3. Gateway

```bash
# Terminal 1
cd DotnetNiger.Identity
dotnet run

# Terminal 2
cd DotnetNiger.Community
dotnet run

# Terminal 3
cd DotnetNiger.Gateway
dotnet run
```

## Ports par defaut (developpement)

- Gateway: http://localhost:5000
- Identity: http://localhost:5075
- Community: http://localhost:5269

## Variables et configuration essentielles

### JWT (obligatoire)

La cle JWT doit etre coherente entre Gateway, Identity et Community.

- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`

### Community

- `Admin:ApiKey` pour les operations admin proteges par cle API custom.
- `IdentityApi:BaseUrl` pour la communication avec Identity.

### Identity

- `ConnectionStrings:DotnetNigerDb`
- `Features:*` pour les toggles fonctionnels
- `AccountDeletion:*` pour le workflow de suppression de compte
- `OAuth:*` pour l'activation des providers externes

## Build et tests

```bash
dotnet build DotnetNiger.slnx
dotnet test DotnetNiger.slnx
```

### Tests de garde d'architecture

```bash
dotnet test DotnetNiger.Architecture.Tests/DotnetNiger.Architecture.Tests.csproj --configuration Release
```

## CI locale rapide (equivalent principal)

```bash
dotnet restore DotnetNiger.slnx
dotnet build DotnetNiger.slnx --configuration Release --no-restore
dotnet test DotnetNiger.slnx --configuration Release --no-build
```
