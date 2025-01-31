# Server Test Project for Unity WebSocket Client Package

This repo contains a test server for my [`Unity WebSocket client package`](https://github.com/mikerochip/unity-websocket)

The test server is written in C# using ASP.NET.

# Setup

1. You have to find the version of .NET that this project is on currently
1. Open [the WebSocketEchoServer.csproj file](./WebSocketEchoServer/WebSocketEchoServer.csproj) in a text editor
1. Look for the `<TargetFramework>` element to find the .NET version
1. Go to https://dotnet.microsoft.com/en-us/download/dotnet and download the SDK for that version
1. Install the SDk
1. Open a terminal
1. Run `dotnet dev-certs https --trust`
1. Now run the server
   * You can open the `WebSocketEchoServer.csproj` in any C# IDE and run it
   * You can also do `dotnet run` in a terminal after changing directories to the `.csproj` directory
1. When you run the server, note the line that says `Now listening on: http://localhost:5182`
2. You'll want to use that for your websocket's server url, but swap `http` for `ws` e.g. `ws://localhost:5182`