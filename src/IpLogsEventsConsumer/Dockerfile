FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/IpLogsEventsConsumer/IpLogsEventsConsumer.csproj", "src/IpLogsEventsConsumer/"]
COPY ["src/IpLogsCommon/IpLogsCommon.csproj", "src/IpLogsCommon/"]
RUN dotnet restore "src/IpLogsEventsConsumer/IpLogsEventsConsumer.csproj"
COPY . .
WORKDIR "/src/src/IpLogsEventsConsumer"
RUN dotnet build "IpLogsEventsConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IpLogsEventsConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IpLogsEventsConsumer.dll"]
