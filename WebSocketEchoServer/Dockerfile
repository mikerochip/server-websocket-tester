FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

COPY . /src
WORKDIR /src

RUN dotnet restore
RUN dotnet publish -c Release -o publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0

COPY --from=build /src/publish /app
WORKDIR /app

ENTRYPOINT ["dotnet", "WebSocketEchoServer.dll"]
