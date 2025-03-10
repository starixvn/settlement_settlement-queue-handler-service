FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder
WORKDIR /src

COPY Sources/Settlement.Consumer/*.csproj ./Sources/Settlement.Consumer/
COPY Sources/Settlement.QueueHandlerService/Settlement.QueueHandlerService.csproj ./Sources/Settlement.QueueHandlerService/

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN --mount=type=cache,target=/root/.nuget/packages \ 
	dotnet restore -r linux-x64 Sources/Settlement.QueueHandlerService/Settlement.QueueHandlerService.csproj
COPY Sources/. ./Sources
WORKDIR /src/Sources/Settlement.QueueHandlerService

RUN --mount=type=cache,target=/root/.nuget/packages \
	dotnet publish -c release -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS base

WORKDIR /app
COPY --from=builder /app .

ENV TZ="America/La_Paz"
ENV COMPlus_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Settlement.QueueHandlerService.dll"]
