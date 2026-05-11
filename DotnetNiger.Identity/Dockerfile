# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DotnetNiger.Identity/DotnetNiger.Identity.csproj", "DotnetNiger.Identity/"]
RUN dotnet restore "DotnetNiger.Identity/DotnetNiger.Identity.csproj"

# Copy all source files
COPY . .
WORKDIR "/src/DotnetNiger.Identity"

# Build
RUN dotnet build "DotnetNiger.Identity.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "DotnetNiger.Identity.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8081

# Create logs directory
RUN mkdir -p /app/logs

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "DotnetNiger.Identity.dll"]
