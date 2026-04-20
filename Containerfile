FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Dyndns.slnx ./
COPY src/Dyndns.Service/Dyndns.Service.csproj src/Dyndns.Service/
COPY test/Dyndns.Service.Tests/Dyndns.Service.Tests.csproj test/Dyndns.Service.Tests/
RUN dotnet restore Dyndns.slnx

COPY . ./
RUN dotnet publish src/Dyndns.Service/Dyndns.Service.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    WebUi__Url=http://0.0.0.0:8080 \
    DYNV6_UPDATER__USERS_FILE_PATH=/data/users.bson \
    Dynv6__LastPublicIpPath=/data/last-public-ip.txt \
    Dynv6__RunHistoryPath=/data/run-history.jsonl \
    Dynv6__RuntimeSettingsPath=/data/dynv6-runtime-settings.json

RUN mkdir -p /data && chown -R app:app /data

EXPOSE 8080
VOLUME ["/data"]

COPY --from=build /app/publish ./

USER app
ENTRYPOINT ["dotnet", "Dyndns.Service.dll"]
