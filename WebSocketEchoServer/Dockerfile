FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

COPY . /src
WORKDIR /src

RUN dotnet restore
RUN dotnet publish -c Release -o publish --no-restore
COPY server-websocket-tester-cert.pfx publish/server-websocket-tester-cert.pfx


FROM mcr.microsoft.com/dotnet/aspnet:9.0

COPY --from=build /src/publish /app
WORKDIR /app

ENV ASPNETCORE_URLS="http://+;https://+"
ENV PFX_FILE_PATH="server-websocket-tester-cert.pfx"

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "WebSocketEchoServer.dll"]
