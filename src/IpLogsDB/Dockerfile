FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src
COPY ["src/IpLogsDB/IpLogsDB.csproj", "src/IpLogsDB/"]
COPY ["src/IpLogsCommon/IpLogsCommon.csproj", "src/IpLogsCommon/"]
RUN dotnet restore "src/IpLogsDB/IpLogsDB.csproj"
COPY . .
WORKDIR "/src/src/IpLogsDB"
RUN dotnet build "IpLogsDB.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IpLogsDB.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IpLogsDB.dll"]
