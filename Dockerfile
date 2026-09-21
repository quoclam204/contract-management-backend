# Build stage using official .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy project files for caching restore layer
COPY ["src/ContractManagement.Api/ContractManagement.Api.csproj", "src/ContractManagement.Api/"]
COPY ["src/ContractManagement.Application/ContractManagement.Application.csproj", "src/ContractManagement.Application/"]
COPY ["src/ContractManagement.Infrastructure/ContractManagement.Infrastructure.csproj", "src/ContractManagement.Infrastructure/"]
COPY ["src/ContractManagement.Domain/ContractManagement.Domain.csproj", "src/ContractManagement.Domain/"]

RUN dotnet restore "src/ContractManagement.Api/ContractManagement.Api.csproj"

# Copy remaining source files
COPY src/ src/

# Build and publish release output
WORKDIR "/app/src/ContractManagement.Api"
RUN dotnet publish "ContractManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage using official .NET 10 ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Configure default port for Render/Cloud PaaS (8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ContractManagement.Api.dll"]
