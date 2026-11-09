# ==============================================================================
# RaceDay South Africa - Web MVC Dockerfile
# Multi-stage production build for ASP.NET Core 8.0 MVC Application
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (cached layer)
COPY ["RaceDay.Web/RaceDay.Web.csproj", "RaceDay.Web/"]
RUN dotnet restore "RaceDay.Web/RaceDay.Web.csproj"

# Copy source code and build
COPY ["RaceDay.Web/", "RaceDay.Web/"]
WORKDIR "/src/RaceDay.Web"
RUN dotnet build "RaceDay.Web.csproj" -c Release -o /app/build

# Publish release assets
RUN dotnet publish "RaceDay.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Copy published outputs from build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
