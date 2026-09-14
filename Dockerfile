# ==============================================================================
# 1. Build Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching dependency restore
COPY ["src/ContractManagement.Api/ContractManagement.Api.csproj", "src/ContractManagement.Api/"]
COPY ["src/ContractManagement.Application/ContractManagement.Application.csproj", "src/ContractManagement.Application/"]
COPY ["src/ContractManagement.Domain/ContractManagement.Domain.csproj", "src/ContractManagement.Domain/"]
COPY ["src/ContractManagement.Infrastructure/ContractManagement.Infrastructure.csproj", "src/ContractManagement.Infrastructure/"]

# Restore NuGet dependencies
RUN dotnet restore "src/ContractManagement.Api/ContractManagement.Api.csproj"

# Copy all source files
COPY src/ src/

# Build and publish the API in Release mode
WORKDIR "/src/src/ContractManagement.Api"
RUN dotnet publish "ContractManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# 2. Runtime Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for container healthcheck
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 5028

ENV ASPNETCORE_URLS=http://+:5028 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ContractManagement.Api.dll"]
