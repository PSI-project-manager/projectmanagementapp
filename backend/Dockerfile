# expected build context: backend/

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build 

WORKDIR /App/

COPY . /App/

RUN dotnet restore

WORKDIR /App/src/Api/

# --no-launch-profile because launchsettings.json overrides env vars set in compose.dev.yaml
# and puts the backend at localhost:8080 instead of 0.0.0.0:8080 in the container.
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
