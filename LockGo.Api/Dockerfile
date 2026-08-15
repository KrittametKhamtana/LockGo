# Build context is this directory (LockGo.Api/, the solution folder) —
# LockGo.Api.csproj references sibling projects via "../LockGo.Application"
# etc., so the build stage needs all of them, not just the Api project.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LockGo.Api/LockGo.Api.csproj LockGo.Api/
COPY LockGo.Application/LockGo.Application.csproj LockGo.Application/
COPY LockGo.Domain/LockGo.Domain.csproj LockGo.Domain/
COPY LockGo.Infrastructure/LockGo.Infrastructure.csproj LockGo.Infrastructure/
RUN dotnet restore LockGo.Api/LockGo.Api.csproj

COPY LockGo.Api/ LockGo.Api/
COPY LockGo.Application/ LockGo.Application/
COPY LockGo.Domain/ LockGo.Domain/
COPY LockGo.Infrastructure/ LockGo.Infrastructure/
RUN dotnet publish LockGo.Api/LockGo.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Runs as a non-root user — the base image ships one out of the box.
USER app

COPY --from=build /app .

# ASP.NET Core's container default port. Config (Database/Jwt/AllowedOrigins)
# comes entirely from environment variables at runtime — see
# docs/DEPLOYMENT.md — nothing environment-specific is baked into this image.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LockGo.Api.dll"]
