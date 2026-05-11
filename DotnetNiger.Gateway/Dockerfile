# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DotnetNiger.Gateway/DotnetNiger.Gateway.csproj", "DotnetNiger.Gateway/"]
RUN dotnet restore "DotnetNiger.Gateway/DotnetNiger.Gateway.csproj"

# Copy source files
COPY . .
WORKDIR "/src/DotnetNiger.Gateway"

# Build the application
RUN dotnet build "DotnetNiger.Gateway.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "DotnetNiger.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Create logs directory
RUN mkdir -p /app/logs

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "DotnetNiger.Gateway.dll"]
