# DotnetNiger.TestIdentity

Minimal OIDC test client for validating the DotnetNiger Identity Server's OpenID Connect flow end-to-end.

## Purpose

This project serves as a **smoke test** for the Identity Server's OIDC implementation. It uses the authorization code flow to authenticate a user and display their claims. Use it to:

- Verify that the Identity Server is running and reachable
- Confirm the OIDC discovery document is correct
- Validate the authorization code flow works end-to-end
- Check that user claims (name, email, roles) are correctly returned

## Quick Start

```bash
cd DotnetNiger.TestIdentity
dotnet run
```

Available at `http://localhost:5200`. Requires the Identity Server (`http://localhost:5075`) to be running.

## Configuration

```json
{
  "Identity": {
    "BaseUrl": "http://localhost:5075",
    "ClientId": "test-identity",
    "ClientSecret": ""
  }
}
```

The `test-identity` client is automatically registered by the Identity Server's database seeder (`DbSeeder.cs`). It must have matching redirect URIs in the Identity Server configuration.

## Tech Stack

- .NET 9.0
- ASP.NET Core Razor Pages
- Cookie + OpenID Connect authentication
