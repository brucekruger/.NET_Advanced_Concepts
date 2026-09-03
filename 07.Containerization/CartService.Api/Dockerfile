# Multi-stage build for Cart Service
# Stage 1: Build stage - compiles the .NET application using the full SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for dependency restoration
# This order is important: copy .csproj files only, so docker can cache this layer
COPY ["CartService.Api/CartService.Api.csproj", "CartService.Api/"]
COPY ["CartService/CartService.csproj", "CartService/"]

# Restore NuGet dependencies - this layer is cached until project files change
# The restore command needs to find all project references
RUN dotnet restore "CartService.Api/CartService.Api.csproj"

# Copy all source code (after restore, so changes to source don't invalidate restore cache)
COPY . .

# Build the application in Release configuration for optimization
RUN dotnet build "CartService.Api/CartService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish stage - creates final artifacts ready for deployment
FROM build AS publish
WORKDIR /src
RUN dotnet publish "CartService.Api/CartService.Api.csproj" -c Release -o /app/publish

# Stage 3: Runtime stage - final lightweight image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl so the docker-compose healthcheck can probe /health
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy only the published artifacts from the publish stage (not build artifacts)
COPY --from=publish /app/publish .

# Expose port 8081 for HTTP traffic
EXPOSE 8081

# Set ASP.NET Core environment to listen on all interfaces on port 8081
ENV ASPNETCORE_URLS=http://+:8081

# Entry point - run the compiled DLL
ENTRYPOINT ["dotnet", "CartService.Api.dll"]
