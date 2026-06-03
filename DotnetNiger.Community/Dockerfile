FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["DotnetNiger.Community/DotnetNiger.Community.csproj", "DotnetNiger.Community/"]
RUN dotnet restore "DotnetNiger.Community/DotnetNiger.Community.csproj"

COPY . .
WORKDIR "/src/DotnetNiger.Community"

RUN dotnet build "DotnetNiger.Community.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotnetNiger.Community.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8082
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8082

RUN mkdir -p /app/logs

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8082/health || exit 1

ENTRYPOINT ["dotnet", "DotnetNiger.Community.dll"]
