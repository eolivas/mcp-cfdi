# Multi-stage Dockerfile for .NET McpCfdi service
# Build context: solution root (e.g., docker build -f src/McpCfdi.Api/Dockerfile .)

# Stage 1: restore — restore NuGet packages
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY Directory.Build.props ./
COPY McpCfdi.sln ./
COPY src/McpCfdi.Domain/McpCfdi.Domain.csproj src/McpCfdi.Domain/
COPY src/McpCfdi.Application/McpCfdi.Application.csproj src/McpCfdi.Application/
COPY src/McpCfdi.Infrastructure/McpCfdi.Infrastructure.csproj src/McpCfdi.Infrastructure/
COPY src/McpCfdi.Api/McpCfdi.Api.csproj src/McpCfdi.Api/
RUN dotnet restore src/McpCfdi.Api/McpCfdi.Api.csproj

# Stage 2: build — compile the application
FROM restore AS build
COPY src/ src/
RUN dotnet build src/McpCfdi.Api/McpCfdi.Api.csproj -c Release --no-restore

# Stage 3: publish — produce a self-contained publish output
FROM build AS publish
RUN dotnet publish src/McpCfdi.Api/McpCfdi.Api.csproj -c Release --no-build -o /app/publish

# Stage 4: final — minimal runtime image with non-root user
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

USER appuser
ENTRYPOINT ["dotnet", "McpCfdi.Api.dll"]
