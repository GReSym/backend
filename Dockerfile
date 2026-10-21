# API image. Built by ../deploy/compose.yaml (profile "app"); the build context is backend/.
# Configuration: appsettings.example.json is shipped as appsettings.json, secrets and hosts come from
# environment variables (ConnectionStrings__DefaultConnection, Jwt__Key, RabbitMq__Host, Ml__BaseUrl, ...).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, so the restore layer is cached until dependencies change
COPY GReSym.Core/GReSym.Core.csproj GReSym.Core/
COPY GReSym.Infrastructure/GReSym.Infrastructure.csproj GReSym.Infrastructure/
COPY GReSym.Application/GReSym.Application.csproj GReSym.Application/
COPY GReSym.API/GReSym.API.csproj GReSym.API/
RUN dotnet restore GReSym.API/GReSym.API.csproj

COPY GReSym.Core/ GReSym.Core/
COPY GReSym.Infrastructure/ GReSym.Infrastructure/
COPY GReSym.Application/ GReSym.Application/
COPY GReSym.API/ GReSym.API/
# chmod: content files keep their host mode (appsettings.example.json may be 600 locally),
# but the runtime image runs as the non-root "app" user, so everything must be world-readable
RUN dotnet publish GReSym.API/GReSym.API.csproj -c Release --no-restore -o /app \
    && cp GReSym.API/appsettings.example.json /app/appsettings.json \
    && chmod -R a+rX /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Serilog writes logs/api-*.log next to the app; the image runs as the non-root "app" user
RUN mkdir -p logs && chown "$APP_UID" logs
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GReSym.API.dll"]
