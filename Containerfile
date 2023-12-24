FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build-env
WORKDIR /app

RUN apt-get update && apt-get install clang zlib1g-dev -y

COPY . ./
RUN dotnet publish src/ConsoleHost/Logship.Agent.ConsoleHost.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["./Logship.Agent.ConsoleHost"]