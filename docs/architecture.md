# Architecture

## Frontend

React SPA in `frontend/`, calling the backend's JSON API.

## Backend

ASP.NET Core Web API in `backend/`, following a classic MVC/layered structure rather than
Clean Architecture — a single `Api` project (`backend/src/Api`) organized as:

- `Controllers/` — `ControllerBase` MVC controllers, the only place that talks HTTP.
- `Services/` — business logic (`IProjectService`/`ProjectService`), each depending directly on
  `AppDbContext`. No repository indirection.
- `Models/` — EF Core entity classes mapped straight to the Postgres schema in `db/schema.sql`.
- `Dtos/` — request/response contracts used at the controller boundary.
- `Validators/` — FluentValidation validators for the DTOs.
- `Data/AppDbContext.cs` — the EF Core `DbContext`.

Tests live in `backend/tests/Api.Tests`, exercising `Services/` against an EF Core InMemory
`AppDbContext`.

New features follow the same pattern: add/extend a `Models/` entity, a `Services/` class, DTOs
and validators, and a `Controllers/` controller exposing it over HTTP.
