# Multi-stage build for Catalog Service
# Stage 1: Build stage - compiles the .NET application using the full SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files needed for the solution
COPY ["CatalogService.Api/CatalogService.Api.csproj", "CatalogService.Api/"]
COPY ["CatalogService.Application/CatalogService.Application.csproj", "CatalogService.Application/"]
COPY ["CatalogService.Domain/CatalogService.Domain.csproj", "CatalogService.Domain/"]
COPY ["CatalogService.Infrastructure/CatalogService.Infrastructure.csproj", "CatalogService.Infrastructure/"]

# Restore NuGet dependencies - this layer is cached until project files change
RUN dotnet restore "CatalogService.Api/CatalogService.Api.csproj"

# Copy all source code
COPY . .

# Build the application in Release configuration for optimization
RUN dotnet build "CatalogService.Api/CatalogService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish stage - creates final artifacts ready for deployment
FROM build AS publish
RUN dotnet publish "CatalogService.Api/CatalogService.Api.csproj" -c Release -o /app/publish

# Stage 3: Runtime stage - final lightweight image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl so the docker-compose healthcheck can probe /health
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy only the published artifacts from the publish stage (not build artifacts)
COPY --from=publish /app/publish .

# Expose port 8080 for HTTP traffic
EXPOSE 8080

# Set ASP.NET Core environment to listen on all interfaces on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Entry point - run the compiled DLL
ENTRYPOINT ["dotnet", "CatalogService.Api.dll"]
