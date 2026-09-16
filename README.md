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

### Rebuild container if u add new dependencies to the project

If you added a new dotnet dependency in some backend layer, you have to rebuild the container:

```
docker compose -f compose.dev.yaml up --build -V --renew-anon-volumes
```

# TODO

- Outline clean architecture rules in docs (Azuolas)
- Add .editorconfig for linter settings (Azuolas)
