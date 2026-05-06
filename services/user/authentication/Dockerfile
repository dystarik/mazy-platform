FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
EXPOSE 7000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/MazyPlatform.Service.User.Authentication.Api/MazyPlatform.Service.User.Authentication.Api.csproj", "src/MazyPlatform.Service.User.Authentication.Api/"]
COPY ["src/MazyPlatform.Service.User.Authentication.Application/MazyPlatform.Service.User.Authentication.Application.csproj", "src/MazyPlatform.Service.User.Authentication.Application/"]
COPY ["src/MazyPlatform.Service.User.Authentication.Domain/MazyPlatform.Service.User.Authentication.Domain.csproj", "src/MazyPlatform.Service.User.Authentication.Domain/"]
COPY ["src/MazyPlatform.Service.User.Authentication.Infrastructure/MazyPlatform.Service.User.Authentication.Infrastructure.csproj", "src/MazyPlatform.Service.User.Authentication.Infrastructure/"]

RUN dotnet restore "src/MazyPlatform.Service.User.Authentication.Api/MazyPlatform.Service.User.Authentication.Api.csproj"

COPY . .

RUN dotnet publish "src/MazyPlatform.Service.User.Authentication.Api/MazyPlatform.Service.User.Authentication.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM build AS migration
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "src/MazyPlatform.Service.User.Authentication.Infrastructure", "--startup-project", "src/MazyPlatform.Service.User.Authentication.Api"]

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8081
ENTRYPOINT ["dotnet", "MazyPlatform.Service.User.Authentication.Api.dll"]
