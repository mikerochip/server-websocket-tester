# Server Test Project for Unity WebSocket Package

This repo contains a test server for my [`Unity WebSocket client package`](https://github.com/mikerochip/unity-websocket)

The test server is written in C# using ASP.NET. This should work on Windows, Mac, and Linux. Yes, C# and ASP.NET are cross platform. It's been like that since 2016. It's really nice.

# Setup

1. Clone this repo
1. You have to find the version of .NET that this project is on currently
1. Open [the WebSocketEchoServer.csproj file](./WebSocketEchoServer/WebSocketEchoServer.csproj) in a text editor
1. Look for the `<TargetFramework>` element to find the .NET version
1. Go to https://dotnet.microsoft.com/en-us/download/dotnet and download the SDK for that version
1. Install the SDK
1. Open a terminal
1. Run `dotnet dev-certs https --trust`
   * If you're having trouble with this step, try [this doc](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide)
1. Now run the server
   * You can open the `WebSocketEchoServer.csproj` in any C# IDE and run it
   * You can also do `dotnet run` in a terminal after changing directories to the `.csproj` directory
1. When you run the server, note the line that says `Now listening on: http://localhost:5182`
1. In Unity, swap the `http` for `ws` then use that as the Url in your `WebSocketConnection` component
   * ex `ws://localhost:5182`
   * or `wss://localhost:7026`