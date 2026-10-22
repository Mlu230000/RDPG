# ==============================================================================
# RaceDay South Africa - REST API Dockerfile with Azure Blob Integration
# Multi-stage production build for ASP.NET Core 8.0 Web API
# ==============================================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RaceDay.Api/RaceDay.Api.csproj", "RaceDay.Api/"]
RUN dotnet restore "RaceDay.Api/RaceDay.Api.csproj"

COPY ["RaceDay.Api/", "RaceDay.Api/"]
WORKDIR "/src/RaceDay.Api"
RUN dotnet build "RaceDay.Api.csproj" -c Release -o /app/build
RUN dotnet publish "RaceDay.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
