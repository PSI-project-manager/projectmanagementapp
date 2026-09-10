{
  description = "Fullstack dev shell: .NET backend + Vite/React frontend";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs { inherit system; };

        dotnet = pkgs.dotnet-sdk_10;
      in
      {
        devShells.default = pkgs.mkShell {
          packages = [
            # --- Backend (.NET) ---
            dotnet

            # --- Frontend (Vite + React) ---
            pkgs.nodejs_22
            pkgs.pnpm
          ];

          env = {
            DOTNET_ROOT = "${dotnet}";
            DOTNET_CLI_TELEMETRY_OPTOUT = "1";
            DOTNET_NOLOGO = "1";
          };

          shellHook = ''
            pnpm --dir frontend install --frozen-lockfile
            dotnet restore backend
            echo "backend:  dotnet $(dotnet --version)"
            echo "frontend: node $(node --version) / pnpm $(pnpm --version)"
          '';
        };
      }
    );
}
