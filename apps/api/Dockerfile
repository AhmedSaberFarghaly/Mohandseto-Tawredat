FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api/Mohandseto.Api.csproj apps/api/
RUN dotnet restore apps/api/Mohandseto.Api.csproj
COPY apps/api/ apps/api/
RUN dotnet publish apps/api/Mohandseto.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /var/lib/mohandseto/data /var/lib/mohandseto/keys \
    && chown -R "$APP_UID:$APP_UID" /var/lib/mohandseto
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
  CMD curl --fail --silent http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "Mohandseto.Api.dll"]
