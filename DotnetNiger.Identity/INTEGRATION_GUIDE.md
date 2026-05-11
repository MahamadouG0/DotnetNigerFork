# Guide d'Intégration — DotnetNiger.Identity

## 🔑 Prérequis

- .NET 9.0+
- Un projet client (API, Web, Mobile)
- Package NuGet : `Microsoft.AspNetCore.Authentication.JwtBearer`

---

## 1. Configuration côté client

### Ajouter l'authentification JWT avec JWKS

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "http://localhost:5075";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
    options.MetadataAddress = "http://localhost:5075/.well-known/openid-configuration";
});

builder.Services.AddAuthorization();
```

> **JWKS (JSON Web Key Set)** : Le endpoint `/.well-known/jwks` expose les clés publiques RSA de OpenIddict. Le client les récupère automatiquement via le `MetadataAddress` pour valider la signature des tokens. Pas besoin de partager une clé secrète.

---

## 2. Endpoints disponibles

### Authentification

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/connect/token` | Échange identifiants contre JWT (form-data) |
| POST | `/api/v1/auth/register` | Créer un compte (body : email, password, firstName, lastName) |
| POST | `/api/v1/auth/confirm-email` | Valider l'email avec le code (body : email, code) |
| GET | `/api/v1/auth/confirm-email` | Valider l'email via lien cliquable (query : email, code) |
| POST | `/api/v1/auth/resend-code` | Renvoyer le code de confirmation |
| POST | `/api/v1/auth/login` | Login JSON (body : email, password, tenantId, rememberMe) |
| POST | `/api/v1/auth/logout` | Déconnexion (token requis) |
| GET | `/api/v1/auth/userinfo` | Infos utilisateur connecté |
| GET | `/api/v1/auth/external-login` | Login via Google/Microsoft/GitHub |
| GET | `/api/v1/auth/external-callback` | Callback après login externe (query : returnUrl, rememberMe) |

### Découverte OpenID Connect

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/.well-known/openid-configuration` | Métadonnées OIDC |
| GET | `/.well-known/jwks` | Clés publiques RSA (JWKS) |

### Utilisateurs, Rôles, Permissions, Tenants, Profil, Admin

(Voir sections ci-dessous)

---

## 3. Flux d'authentification

### 3.1 Inscription avec confirmation email

```http
# 1. Créer le compte
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "MonMotDePasse@123",
  "firstName": "Jean",
  "lastName": "Dupont"
}
```

Réponse :
```json
{
  "message": "Compte créé. Un code de confirmation vous a été envoyé par email.",
  "userId": "guid-...",
  "email": "user@example.com",
  "code": "A3F9K2"
}
```

> Le code `code` n'est retourné que si SMTP n'est pas configuré (dev). En production, il est envoyé par email.

### 3.2 Confirmer l'email (2 façons)

**Méthode 1 — Lien cliquable** : L'email contient un bouton "Confirmer mon compte" qui redirige vers :
```
GET /api/v1/auth/confirm-email?email=user@example.com&code=A3F9K2
```

**Méthode 2 — Code manuel** (depuis une app mobile ou une API) :
```http
POST /api/v1/auth/confirm-email
Content-Type: application/json

{
  "email": "user@example.com",
  "code": "A3F9K2"
}
```

### 3.3 Renvoyer le code

```http
POST /api/v1/auth/resend-code
Content-Type: application/json

{
  "email": "user@example.com"
}
```

### 3.4 Obtenir un JWT

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&username=user@example.com&password=MonMotDePasse@123&scope=openid+profile+email+roles+offline_access&remember_me=true
```

Paramètres :

| Paramètre | Requis | Description |
|-----------|--------|-------------|
| `grant_type` | Oui | `password` ou `refresh_token` |
| `username` | Oui | Email de l'utilisateur (pour password grant) |
| `password` | Oui | Mot de passe (pour password grant) |
| `scope` | Non | Espaces séparés par `+`. Valeurs : `openid`, `profile`, `email`, `roles`, `api`, `offline_access` |
| `remember_me` | Non | `true` → token valable 7 jours. `false` ou absent → 1 heure |

**Réponse (password grant)** :
```json
{
  "access_token": "eyJhbG...",
  "token_type": "Bearer",
  "expires_in": 604799,
  "refresh_token": "eyJhbG..."
}
```

> `expires_in` = 3600 (1h) sans `remember_me`, 604799 (7 jours) avec `remember_me=true`.
> Le `refresh_token` est retourné uniquement si le scope `offline_access` est demandé.

### 3.5 Rafraîchir le token

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&refresh_token=eyJhbG...&scope=openid+profile+email+roles+offline_access
```

| Paramètre | Requis | Description |
|-----------|--------|-------------|
| `grant_type` | Oui | `refresh_token` |
| `refresh_token` | Oui | Le refresh token obtenu précédemment |
| `scope` | Non | Doit correspondre aux scopes originaux |

**Réponse** : Identique au password grant (nouvel access_token + nouveau refresh_token).

### 3.6 Login JSON (validation des identifiants)

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "MonMotDePasse@123",
  "rememberMe": true
}
```

Réponse :
```json
{
  "id": "guid-...",
  "email": "user@example.com",
  "firstName": "Jean",
  "lastName": "Dupont",
  "avatarUrl": null,
  "tenantId": "guid-...",
  "isActive": true,
  "roles": ["User"],
  "permissions": [],
  "rememberMe": true
}
```

> Cet endpoint ne retourne pas de JWT. Il valide les identifiants et retourne les infos utilisateur.
> Utilisez `/connect/token` pour obtenir un JWT.

### 3.7 Login social (Google, Microsoft, GitHub)

**Étape 1** — Rediriger l'utilisateur vers le provider :
```
GET /api/v1/auth/external-login?provider=Google
```

**Étape 2** — Le provider redirige vers `/api/v1/auth/external-callback`

**Étape 3** — Le callback retourne les infos utilisateur :
```json
{
  "id": "guid",
  "email": "user@gmail.com",
  "firstName": "Jean",
  "lastName": "Dupont",
  "avatarUrl": null,
  "tenantId": null,
  "isActive": true,
  "roles": ["User"],
  "permissions": []
}
```

> **Important** : Pour un flow navigateur, le callback doit rediriger vers votre frontend. Configurez `returnUrl` dans l'appel initial. Le JWT doit être obtenu via `/connect/token` avec le grant `password` après création du compte social.

---

## 4. Isolation multi-tenant

Chaque requête est automatiquement filtrée par `TenantId` via :
- Le claim `tenant_id` dans le JWT
- Le header `X-Tenant-Id` pour les endpoints publics

---

## 5. Exemple complet (cURL)

```bash
# 1. Créer un compte
REG=$(curl -s -X POST http://localhost:5075/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo@123456","firstName":"Demo","lastName":"User"}')
echo "$REG" | jq .
CODE=$(echo "$REG" | jq -r '.code')

# 2. Confirmer l'email
curl -s -X POST http://localhost:5075/api/v1/auth/confirm-email \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"demo@test.com\",\"code\":\"$CODE\"}" | jq .

# 3. Login (validation des identifiants)
curl -s -X POST http://localhost:5075/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo@123456","rememberMe":true}' | jq .

# 4. Obtenir un JWT (avec refresh token)
TOKEN_RESP=$(curl -s -X POST http://localhost:5075/connect/token \
  -d "grant_type=password&username=demo@test.com&password=Demo@123456&scope=openid+profile+email+roles+offline_access&remember_me=true")
ACCESS_TOKEN=$(echo "$TOKEN_RESP" | jq -r '.access_token')
REFRESH_TOKEN=$(echo "$TOKEN_RESP" | jq -r '.refresh_token')

# 5. Appeler les endpoints protégés
curl -s http://localhost:5075/api/v1/auth/userinfo \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .

curl -s http://localhost:5075/api/v1/profile \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .

# 6. Rafraîchir le token
curl -s -X POST http://localhost:5075/connect/token \
  -d "grant_type=refresh_token&refresh_token=$REFRESH_TOKEN&scope=openid+profile+email+roles+offline_access" | jq .
```

---

## 6. Configuration SMTP (emails réels)

Dans `appsettings.json` ou `user-secrets` :

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "votre@email.com",
    "Password": "votre-mot-de-passe",
    "FromEmail": "noreply@dotnetniger.com",
    "FromName": "DotnetNiger",
    "AppBaseUrl": "http://localhost:5075"
  }
}
```

> Si `Host` est vide (défaut), l'email est loggé dans la console — pas d'envoi réel. Le code de confirmation est alors retourné directement dans la réponse de `/register`.

> `AppBaseUrl` est utilisé pour construire le lien de confirmation dans l'email. Remplacez-la par l'URL réelle de votre déploiement en production.

---

## 7. Création des API Keys OAuth

### Google
1. Aller sur https://console.cloud.google.com/apis/credentials
2. Créer un projet ou sélectionner un projet existant
3. Aller dans **APIs & Services** → **Credentials**
4. Cliquer **Create Credentials** → **OAuth client ID**
5. **Application type** : Web application
6. **Authorized redirect URIs** : `http://localhost:5075/signin-google`
7. Copier le **Client ID** et **Client Secret**

### Microsoft
1. Aller sur https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps
2. Cliquer **New registration**
3. **Redirect URI** : `http://localhost:5075/signin-microsoft`
4. Après création, noter le **Application (client) ID**
5. Aller dans **Certificates & secrets** → **New client secret**
6. Copier le **Client Secret**

### GitHub
1. Aller sur https://github.com/settings/developers
2. Cliquer **New OAuth App**
3. **Homepage URL** : `http://localhost:5075`
4. **Authorization callback URL** : `http://localhost:5075/signin-github`
5. Copier le **Client ID** et **Client Secret**

### Activer les providers

Dans `appsettings.Development.json` ou `user-secrets` :

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "votre-id-google",
      "ClientSecret": "votre-secret-google"
    },
    "Microsoft": {
      "ClientId": "votre-id-microsoft",
      "ClientSecret": "votre-secret-microsoft"
    },
    "GitHub": {
      "ClientId": "votre-id-github",
      "ClientSecret": "votre-secret-github"
    }
  }
}
```

Les providers ne sont activés que si leur `ClientId` est présent et non vide.

---

## 8. JWKS — JSON Web Key Set

JWKS est un standard qui expose les clés publiques servant à vérifier la signature des JWT.

**DotnetNiger.Identity expose automatiquement** :
- `/.well-known/openid-configuration` : métadonnées OIDC
- `/.well-known/jwks` : clés publiques RSA

**Avantages JWKS** :
- ✅ Pas besoin de partager une clé secrète
- ✅ Rotation de clés possible sans downtime
- ✅ Standard OpenID Connect

**En développement**, OpenIddict utilise des clés éphémères (change à chaque redémarrage). Pour une rotation persistante, configurez `AddSigningKey()` avec un certificat.

---

## 9. Tests

Compte admin plateforme :
- **Email :** `admin@dotnetniger.com`
- **Mot de passe :** `Admin@123456`
- **Rôle :** Admin

---

## 10. Swagger

L'interface Swagger est disponible en développement à :
```
http://localhost:5075/swagger
```
