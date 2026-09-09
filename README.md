# Development: dev environment

TODO: explain how to eneter the created dev environment (Pijus)

# Development: branching strategy

At the start of every sprint:

- Create a feature branch from main based on your user story
- Implement the feature
- Merge to main and resolve merge conflicts with teammates if any

# Development: running the app for interactive development

### Before

Make sure you have a `.env` file created locally from the example of `env.example`. For local
development u can just copy it.

### Running the app interactively

From the root of the project do:

```
docker compose -f compose.dev.yaml up -V
```

You should see:

- backend docs at: `http://localhost:8080/scalar/`
- frontend at: `http://localhost:5173/` (not yet setup)

### Rebuild container if u add new dependencies to the project

If you added a new dotnet dependency in some backend layer, you have to rebuild the container:

```
docker compose -f compose.dev.yaml up --build
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

- Create a devcontainer with dotnet 10 sdk, formatter (csharpier) with the version derived from
  `backend/dotnet-tools.json` (Pijus)
- Bootstrap frontend with react and vite, containerize frontend for interactive development
  (`npm run dev` i think), add `.dockerignore` and `.gitignore` for node_modules and other stuff u
  dont want to be present in docker builds and github. Add frontend service to `compose.dev.yaml`,
  expose frontend at `http://localhost:5173/` (Pijus)
- Create and put database schema at `db/schema.sql`, add the database as a service in
  `compose.dev.yaml`, add `compose.dev.yaml` and set up the backend to reach it with efcore (dotnet
  scaffold), also add a healthcheck to db service and depends_on to backend so that backend only
  starts when db is healthy, add the `db/schema.sql` as an entrypoint script that the db container
  executes on volume creation (only re runs it if the db data dir is empty) (Pijus)
- Setup GitHub projects with user stories (Andrius)
- Design UI (Justinas)
- Create GitHub workflows for linting and formatting checks on PR/push (Roslyn, CSharpier) (Azuolas)
- Outline clean architecture rules in docs (Azuolas)

checklist:

- frontend and backend have their own .gitignores
- frontend and backend have their own Dockerfile and .dockerignore (so backend doesnt copy obj/ bin/
  and frontend node_modules, etc. into builds)
