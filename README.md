# Development: dev environment

### Using nix (recommended)

Install Nix determinate installer from `https://docs.determinate.systems/`

Open a new shell and run `nix develop` from project root

## Set it up yourself

### Pre-setup

Make sure you have a `.env` file created locally from the example of `.env.example`. For local
development u can just copy it.

### Dependency installations

Install and put on PATH:

- dotnet 10 sdk
- node 22
- pnpm
- restore dotnet packages from `backend/`
- install node packages from `frontend/`
- install csharpier formatter by doing `dotnet tool restore` from `backend/`

### Running the app interactively

From the root of the project do:

```
docker compose -f compose.dev.yaml up -V
```

You should see:

- backend docs at: `http://localhost:8080/scalar/`
- frontend at: `http://localhost:5173/`

### Database schema (EF Core code-first)

The schema is owned by EF Core migrations in `backend/src/Api/Data/Migrations`. They are applied automatically on startup in Development.

After changing a model or `AppDbContext`, from `backend/src/Api` run `dotnet ef migrations add <Name>` and commit the generated files.

If you have an old DB volume created from the former `db/schema.sql`, recreate it once:

```
docker compose -f compose.dev.yaml down -v
```

### Rebuild container if u add new dependencies to the project

If you added a new dotnet dependency in some backend layer, you have to rebuild the container:

```
docker compose -f compose.dev.yaml up --build -V --renew-anon-volumes
```

### Seeded accounts (local dev DB only):

Admin:

- Email: admin@test.local
- Password: TestAdmin123!

Contributor:

- Email: contributor@test.local
- Password: Test1234!

Admin + Contributor:

- Email: admincontributor@test.local
- Password: Test1234!
