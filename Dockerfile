FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder
WORKDIR /src

COPY ./Sources/ThirdPartyBroadcast.Consumer ./ThirdPartyBroadcast.Consumer
COPY ./Sources/ThirdPartyBroadcast.Consumer.Service/ThirdPartyBroadcast.Consumer.Service.csproj ./ThirdPartyBroadcast.Consumer.Service/ThirdPartyBroadcast.Consumer.Service.csproj

WORKDIR /src/ThirdPartyBroadcast.Consumer.Service
RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN --mount=type=cache,target=/root/.nuget/packages \ 
	dotnet restore -v n -r linux-x64

COPY ./Sources/ThirdPartyBroadcast.Consumer.Service .

RUN --mount=type=cache,target=/root/.nuget/packages \
	dotnet publish -r linux-x64 --self-contained false -c Release -o /app 

FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS base

WORKDIR /app
COPY --from=builder /app .

ENV TZ="America/La_Paz"
ENV COMPlus_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "ThirdPartyBroadcast.Consumer.Service.dll"]
