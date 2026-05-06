FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/MazyPlatform.Service.Gateway/MazyPlatform.Service.Gateway.csproj", "src/MazyPlatform.Service.Gateway/"]

RUN dotnet restore "src/MazyPlatform.Service.Gateway/MazyPlatform.Service.Gateway.csproj"

COPY . .

RUN dotnet publish "src/MazyPlatform.Service.Gateway/MazyPlatform.Service.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MazyPlatform.Service.Gateway.dll"]
