FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

RUN apt-get update && apt-get install clang zlib1g-dev -y

COPY . ./
RUN dotnet publish src/ConsoleHost/Logship.Agent.ConsoleHost.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["./Logship.Agent.ConsoleHost"]