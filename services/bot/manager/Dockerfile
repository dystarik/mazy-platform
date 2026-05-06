FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
EXPOSE 5101
EXPOSE 5102

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/MazyPlatform.Service.Bot.Manager.Api/MazyPlatform.Service.Bot.Manager.Api.csproj", "src/MazyPlatform.Service.Bot.Manager.Api/"]
COPY ["src/MazyPlatform.Service.Bot.Manager.Application/MazyPlatform.Service.Bot.Manager.Application.csproj", "src/MazyPlatform.Service.Bot.Manager.Application/"]
COPY ["src/MazyPlatform.Service.Bot.Manager.Domain/MazyPlatform.Service.Bot.Manager.Domain.csproj", "src/MazyPlatform.Service.Bot.Manager.Domain/"]
COPY ["src/MazyPlatform.Service.Bot.Manager.Infrastructure/MazyPlatform.Service.Bot.Manager.Infrastructure.csproj", "src/MazyPlatform.Service.Bot.Manager.Infrastructure/"]

RUN dotnet restore "src/MazyPlatform.Service.Bot.Manager.Api/MazyPlatform.Service.Bot.Manager.Api.csproj"

COPY . .

RUN dotnet publish "src/MazyPlatform.Service.Bot.Manager.Api/MazyPlatform.Service.Bot.Manager.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM build AS migration
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "src/MazyPlatform.Service.Bot.Manager.Infrastructure", "--startup-project", "src/MazyPlatform.Service.Bot.Manager.Api"]

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MazyPlatform.Service.Bot.Manager.Api.dll"]
