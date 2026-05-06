FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
EXPOSE 7000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/MazyPlatform.Service.Scenario.Repository.Api/MazyPlatform.Service.Scenario.Repository.Api.csproj", "src/MazyPlatform.Service.Scenario.Repository.Api/"]
COPY ["src/MazyPlatform.Service.Scenario.Repository.Application/MazyPlatform.Service.Scenario.Repository.Application.csproj", "src/MazyPlatform.Service.Scenario.Repository.Application/"]
COPY ["src/MazyPlatform.Service.Scenario.Repository.Domain/MazyPlatform.Service.Scenario.Repository.Domain.csproj", "src/MazyPlatform.Service.Scenario.Repository.Domain/"]
COPY ["src/MazyPlatform.Service.Scenario.Repository.Infrastructure/MazyPlatform.Service.Scenario.Repository.Infrastructure.csproj", "src/MazyPlatform.Service.Scenario.Repository.Infrastructure/"]

RUN dotnet restore "src/MazyPlatform.Service.Scenario.Repository.Api/MazyPlatform.Service.Scenario.Repository.Api.csproj"

COPY . .

RUN dotnet publish "src/MazyPlatform.Service.Scenario.Repository.Api/MazyPlatform.Service.Scenario.Repository.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM build AS migration
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "src/MazyPlatform.Service.Scenario.Repository.Infrastructure", "--startup-project", "src/MazyPlatform.Service.Scenario.Repository.Api"]

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8083
ENTRYPOINT ["dotnet", "MazyPlatform.Service.Scenario.Repository.Api.dll"]
