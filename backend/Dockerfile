FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ContextIQ.Api/ContextIQ.Api.csproj backend/ContextIQ.Api/
RUN dotnet restore backend/ContextIQ.Api/ContextIQ.Api.csproj
COPY ai-service/data/catalog.json ai-service/data/catalog.json
COPY backend/ContextIQ.Api/ backend/ContextIQ.Api/
RUN dotnet publish backend/ContextIQ.Api/ContextIQ.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContextIQ.Api.dll"]
