FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/IpLogsAPI/IpLogsAPI.csproj", "src/IpLogsAPI/"]
COPY ["src/IpLogsCommon/IpLogsCommon.csproj", "src/IpLogsCommon/"]
RUN dotnet restore "src/IpLogsAPI/IpLogsAPI.csproj"
COPY . .
WORKDIR "/src/src/IpLogsAPI"
RUN dotnet build "IpLogsAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IpLogsAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IpLogsAPI.dll"]
