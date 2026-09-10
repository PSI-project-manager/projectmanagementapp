# Development: dev environment

### Using nix (recommended)

Install Nix determinate installer from `https://docs.determinate.systems/`

Open a new shell and run `nix develop` from project root

Pros:

- environment is setup for you automatically (installs dotnet 10 sdk, node 22, pnpm, restores dotnet
  packages, installs node packages) and you can start developing immediately
- the environment is the same between everyone so there's no "it doesn't work on my machine"

Cons:

- have to install extra tool

## Set it up yourself

Install and put on PATH:

- dotnet 10 sdk
- node 22
- pnpm
- restore dotnet packages from `backend/`
- install node packages from `frontend/`
- install csharpier formatter by doing `dotnet tool restore` from `backend/`

Pros:

- don't have to install extra tool

Cons:

- takes longer to setup
- not the same environment between everyone, so you might have issues that other's don't

# Development: branching strategy

IMPORTANT:

- Never rewrite history that has already been pushed to a shared branch

At the start of every sprint:

- Create a feature branch from main based on your user story
- Implement the feature
- Run the formatter to format the code `dotnet csharpier format .` from inside `backend/`
- Run the linter and fix all the code errors: run `dotnet format style --verify-no-changes` and
  `dotnet format analyzers --verify-no-changes` from inside `backend/`(github will reject the PR if
  you don't)
- Merge to main and resolve merge conflicts with teammates if any

# Development: running the app for interactive development

### Before

Make sure you have a `.env` file created locally from the example of `.env.example`. For local
development u can just copy it.

### Running the app interactively

From the root of the project do:

```
docker compose -f compose.dev.yaml up -V
```

You should see:

- backend docs at: `http://localhost:8080/scalar/`
- frontend at: `http://localhost:5173/`

### Rebuild container if u add new dependencies to the project

If you added a new dotnet dependency in some backend layer, you have to rebuild the container:

```
docker compose -f compose.dev.yaml up --build -V --renew-anon-volumes
```

# Architecture

Read `docs/architecture/`

# GitHub workflows

TODO (Azuolas)

- some linter check gates every pr/push
- some formatter check gates every pr/push

# Documentation

Generated API documentation at `http://localhost:8080/scalar/`

Other documentation in `docs/`

# TODO

- Create and put database schema at `db/schema.sql`, add the database as a service in
  `compose.dev.yaml`, add `compose.dev.yaml` and set up the backend to reach it with efcore (dotnet
  scaffold), also add a healthcheck to db service and depends_on to backend so that backend only
  starts when db is healthy, add the `db/schema.sql` as an entrypoint script that the db container
  executes on volume creation (only re runs it if the db data dir is empty) (Pijus)
- Create GitHub workflows for linting and formatting checks on PR/push (Roslyn, CSharpier) (Azuolas)
- Outline clean architecture rules in docs (Azuolas)
- Add .editorconfig for linter settings (Azuolas)
