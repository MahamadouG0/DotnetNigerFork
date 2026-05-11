# API Reference (Operationnelle)

## Base URLs (dev)

- Gateway: http://localhost:5000
- Identity (direct): http://localhost:5075
- Community (direct): http://localhost:5269

## Swagger

- Gateway: http://localhost:5000/swagger
- Identity: http://localhost:5075/swagger
- Community: http://localhost:5269/swagger

## Endpoints sante

### Gateway

- `GET /health`
- `GET /health/downstream`
- `GET /health/ready`
- `GET /metrics/latency`

### Identity

- `GET /api/v1/diagnostics/ping`
- `GET /api/v1/diagnostics/health`

### Community

- `GET /api/v1/test/health`

## Conventions de versioning

Identity et Community utilisent des routes versionnees:

- `/api/v1/...`

Le Gateway expose des routes simplifiees et les mappe vers les routes aval versionnees.

## Exemples de mapping Gateway

Exemples observes dans [DotnetNiger.Gateway/ocelot.json](../DotnetNiger.Gateway/ocelot.json):

- `/api/auth/{everything}` -> Identity `/api/v1/auth/{everything}`
- `/api/me/{everything}` -> Identity `/api/v1/me/{everything}`
- `/api/tokens/{everything}` -> Identity `/api/v1/tokens/{everything}`
- `/api/identity/admin/{everything}` -> Identity `/api/v1/admin/{everything}`
- `/api/diagnostics/{everything}` -> Identity `/api/v1/diagnostics/{everything}`
- `/api/test/{everything}` -> Community `/api/v1/test/{everything}`
- `/api/newsletters/subscribe` -> Community `/api/v1/newsletters/subscribe`

## Authentification

- JWT Bearer pour les routes protegees.
- Certains endpoints admin/techniques utilisent des mecanismes complementaires (ex: cle API selon contexte service).

## Headers utiles

- `Authorization: Bearer <token>`
- `X-Request-ID` (propage par Gateway)
- `ClientId` / `Oc-Client` (rate limiting Ocelot)
