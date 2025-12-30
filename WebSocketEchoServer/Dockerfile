ARG PfxFileName="websocket-tester.pfx"


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PfxFileName

COPY . /src
WORKDIR /src

RUN dotnet restore
RUN dotnet publish -c Release -o publish --no-restore
COPY $PfxFileName publish/$PfxFileName


FROM mcr.microsoft.com/dotnet/aspnet:9.0
ARG PfxFileName

COPY --from=build /src/publish /app
WORKDIR /app

ENV ASPNETCORE_URLS="http://+"
#ENV ASPNETCORE_URLS="http://+;https://+"
ENV PFX_FILE_PATH="$PfxFileName"

EXPOSE 80
#EXPOSE 443

ENTRYPOINT ["dotnet", "WebSocketEchoServer.dll"]
