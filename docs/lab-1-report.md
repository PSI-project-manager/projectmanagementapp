# Lab 1 Report — Project Manager

Team: PSI-project-manager · Repository: `PSI-project-manager/projectmanagementapp`
Deadline: week 7 · Max points: 1.5 · Status as of 2026-10-16

## 1. What we built

A web-based issue tracker (a small Jira-style tool) where admins manage users, projects and item
types, and contributors create and browse items inside the projects they are authorized for.

| Layer    | Technology                                                                 |
| -------- | -------------------------------------------------------------------------- |
| Frontend | React + TypeScript SPA (Vite, React Router, ESLint)                        |
| Backend  | ASP.NET Core Web API (.NET 10), layered MVC: Controllers → Services → EF Core |
| Database | PostgreSQL, schema in `db/schema.sql`, dev data via `DevDataSeeder`        |
| Auth     | JWT bearer tokens; roles (`Admin`, `Contributor`) are carried in the token |
| Tooling  | Docker Compose dev environment, Nix flake, CSharpier, `dotnet format`, GitHub Actions (lint + format) |

Architecture details: [`architecture.md`](architecture.md). Branching and review workflow:
[`development-strategy.md`](development-strategy.md).

## 2. End-to-end demo scenario

1. Start the stack: `docker compose -f compose.dev.yaml up -V` (frontend `:5173`, API docs `:8080/scalar/`).
2. Open the app unauthenticated → redirected to `/login` (protected routes).
3. Log in as **admin** (`admin@test.local`) → create a user, assign roles (`/admin/users`), create a
   project (`/projects`), configure an item type (`/admin/item-types`).
4. Log in as **contributor** (`contributor@test.local`) → see only the projects they are a member of,
   create an item in one (`/items/new`) and see it in the project's item list.
5. As the contributor, open `/admin/users` → redirected to the "forbidden" page.
6. Bulk import: `POST /api/projects/{id}/items/import` with `Content-Type: text/csv` returns a per-row
   report (created / failed, with line numbers and errors).

## 3. User stories (10+ defined, 3+ implemented)

All stories with acceptance criteria are in [`user-stories.md`](user-stories.md): 18 stories across
5 sprints (US-01 … US-18). Sprint plan: [`sprint-structure.md`](sprint-structure.md).

| Story | Title                         | State in `main`                                                                 |
| ----- | ----------------------------- | ------------------------------------------------------------------------------- |
| US-01 | User login                    | Implemented — `AuthController`, `LoginPage`, JWT, protected routes              |
| US-03 | Create and manage users       | Implemented — `UsersController` (admin only), `UsersPage`, activate/deactivate  |
| US-04 | Assign roles to users         | Implemented — `RoleEditor`, roles in JWT, `[Authorize(Roles=…)]`, forbidden page |
| US-05 | Create and manage projects    | Implemented — `ProjectsController`; contributors see only their own projects    |
| US-07 | Define item types             | Implemented — `ItemTypesController`, `ItemTypesPage`, inactive types blocked    |
| US-09 | Create item                   | Implemented — `ItemsController`, validators, `ItemForm`                         |
| Other | US-?? | Planned for later sprints, not part of this lab                            |

That is 6+ stories fully implemented, against the requirement of 3.

## 4. Formal requirements — how each is met

| Requirement | Where / how |
| ----------- | ----------- |
| Interface + end-to-end scenario | React UI, scenario in section 2 |
| Own class, struct, record, enum; 1 immutable | `backend/src/Api/Imports/`: `ImportReport` (class), `CsvLine` (**readonly struct**, immutable), `ImportRowResult` (**record**, immutable), `ImportOutcome` (enum) |
| Properties in struct and class | `CsvLine.LineNumber`, `Text`, `IsBlank`; `ImportReport.TotalCount`, `CreatedCount`, `FailedCount` |
| Named and optional arguments | Optional: `ImportRowResult(int LineNumber, ImportOutcome Outcome, string? Title = null, int? ItemId = null, string? Error = null)`. Named: `new(lineNumber, ImportOutcome.Created, Title: …, ItemId: …)`, `new CreateItemRequest(Title: …, Description: …, ProjectId: …, …)` in `ItemImportService`, `detectEncodingFromByteOrderMarks: true` in `CsvStreamExtensions` |
| Extension methods | `Stream.ReadCsvLinesAsync` (`CsvStreamExtensions`), `ClaimsPrincipal.GetUserId` (`ClaimsPrincipalExtensions`) |
| Iterating collections correctly | `await foreach` over the CSV in `ItemImportService.ImportAsync`; `foreach` in `ImportReportDto.FromReport` |
| Stream to load data | The CSV request body (`Request.Body`) is read line by line as a `Stream` via `ReadCsvLinesAsync` — never loaded fully into memory; size capped by `[RequestSizeLimit]` |
| LINQ to Objects | `ImportReport.CreatedCount/FailedCount` (`Count` with predicate); `ItemImportService.Describe` (`Any`, `Select`); `ProjectService.ListAsync` (`Where`, `Any`, `OrderBy`, `Select`); `UserService`, `ItemService`, `ItemTypeService` (`Where` / `OrderBy` / `Select`; those that run against the DB are LINQ to Entities) |
| Standard .NET interface | `ImportReport : IEnumerable<ImportRowResult>`, `CsvLine : IEquatable<CsvLine>` |
| Pull requests with descriptions, ≥3 authored and ≥3 reviewed per member | See section 5 |
| Uniform coding style | Backend: CSharpier + `dotnet format style/analyzers` enforced by the `format.yml` workflow; frontend: ESLint (`lint.yml`) and shared `eslint.config.js`; rules documented in `development-strategy.md` |
| 10+ user stories with acceptance criteria, 3+ implemented | Section 3 |

## 5. Teamwork and GitHub process

- Every change goes through a feature branch (`US-xx_…`, `feature/…`) and a pull request into `main`;
  the repo has 20+ merged pull requests.
- Branch naming follows the user story ID, so the history is traceable to the backlog.
- CI checks formatting and linting on pull requests.
- Merge conflicts were resolved by the authors (for example, the move from role lookups in the DB to
  roles in the JWT during the merge of PR #48).

Contributors with commits on `main` (from `git shortlog`): PijusRan, azuolasma / azumawin,
AndriusSurvila, justas2006.

Per-member counts for the "≥3 authored and ≥3 reviewed PRs" requirement:

| Member | Authored merged PRs | Reviewed PRs (with comments) |
| ------ | ------------------- | ---------------------------- |
| Andrius Survila | 4 | 4 |
| Pijus Ranonis | 9 | 6 |
| Ąžuolas M. | 4 | 3 |
| Justas | 4 | 1 |

> Review counts cannot be derived from git history alone, they come from the PR pages
> (Pull requests → filter `reviewed-by:<user>`).

## 6. Lab goals

- **Web app with lecture material (1–6):** done — types, properties, extension methods, LINQ,
  collections, streams, interfaces are all used in real features.
- **Basic ASP.NET app:** done — layered API with validation (FluentValidation), EF Core, JWT auth.
- **GitHub and team coding:** feature branches, PRs, CI checks, documented strategy.
- **Foundation for future work:** Docker/Nix dev environment, schema, seeded data, backlog of
  18 stories planned across 5 sprints.
- **Code review:** feedback in PRs and follow-up fixes (e.g. removing authorization from
  `ItemsController`, lint and format fixes).

## 7. Known gaps and next steps

- No logout flow yet (US-02) and no status management (US-08).
- Item editing and detail view (US-11, US-12), project membership management (US-06),
  filters, search and board are planned for sprints 3–5.
- The CSV import has an API only; there is no UI for it yet.
- Automated tests are described in `architecture.md` (`backend/tests/Api.Tests`) but the test
  project is not in the repository yet.
- No refresh tokens, instead our JWTs are 8 hours long
