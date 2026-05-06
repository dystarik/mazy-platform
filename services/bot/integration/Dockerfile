FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=9090
ENV DOTNET_EnableDiagnostics=0
EXPOSE 9090

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/MazyPlatform.Service.Bot.Integration/MazyPlatform.Service.Bot.Integration.csproj", "src/MazyPlatform.Service.Bot.Integration/"]

RUN dotnet restore "src/MazyPlatform.Service.Bot.Integration/MazyPlatform.Service.Bot.Integration.csproj"

COPY . .

RUN dotnet publish "src/MazyPlatform.Service.Bot.Integration/MazyPlatform.Service.Bot.Integration.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MazyPlatform.Service.Bot.Integration.dll"]
