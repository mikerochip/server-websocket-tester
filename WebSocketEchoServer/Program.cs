using System.Net;
using System.Net.WebSockets;
using System.Text;

#region Config Constants
const int maxEchoesUntilForcedMessage = -1;
const bool binaryForcedMessage = false;
const WebSocketMessageType forcedMessageType = binaryForcedMessage ? WebSocketMessageType.Binary : WebSocketMessageType.Text;

const bool initiateCloseAfterConnect = false;
#endregion

#region Startup
Console.Title = "WebSocket Server";

var loggerFactory = LoggerFactory.Create(loggingBuilder =>
    loggingBuilder.AddSimpleConsole(options =>
    {
        options.IncludeScopes = false;
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss.ffff ";
    }));
var logger = loggerFactory.CreateLogger("Echo");
#endregion

#region WebApplication Setup
var builder = WebApplication.CreateBuilder();

// configure kestrel with bundled cert iff cert vars are set
var pfxFilePath = Environment.GetEnvironmentVariable("PFX_FILE_PATH");
var pfxPassword = Environment.GetEnvironmentVariable("PFX_PASSWORD");
logger.LogInformation("PFX_FILE_PATH is: \"{CertFilePath}\"", pfxFilePath);
logger.LogInformation("PFX_PASSWORD set: {CertPasswordDetected}", !string.IsNullOrEmpty(pfxPassword));
if (!string.IsNullOrEmpty(pfxFilePath) && !string.IsNullOrEmpty(pfxPassword))
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(80);
        serverOptions.ListenAnyIP(443, listenOptions =>
        {
            listenOptions.UseHttps(pfxFilePath, pfxPassword);
        });
    });
}

var app = builder.Build();
app.UseHttpsRedirection();
app.UseWebSockets();
#endregion

#region Routes
app.Map("/hello", async context => await context.Response.WriteAsync("Hello!"));

ulong nextSocketId = 0;
app.Map("/", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socketId = nextSocketId == ulong.MaxValue ? nextSocketId = 1 : ++nextSocketId;

        logger.LogInformation("[{SocketId}] Conn: {ConnectionId}", socketId, context.Connection.Id);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        try
        {
            if (initiateCloseAfterConnect)
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            else
                await EchoAsync(socketId, webSocket);
        }
        catch (WebSocketException e)
        {
            if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                throw;
        }

        logger.LogInformation("[{SocketId}] DC: {ConnectionId}", socketId, context.Connection.Id);
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});
#endregion

#region Main
await app.RunAsync();
return;
#endregion

#region Helper Methods
async Task EchoAsync(ulong socketId, WebSocket webSocket)
{
    const int maxMessageSize = 1024 * 4;

    // make this less than 0 to not send forced messages
    var echoesUntilForcedMessage = 0;

    string forcedMessage;
    byte[] forcedMessageBuffer;
    if (binaryForcedMessage)
    {
        // sample message from https://github.com/mikerochip/unity-websocket/issues/25#issuecomment-2610624605
        forcedMessage = "kwWaAQAAAAAAAACRAJCWlZIAzPCSAc0B4JICzQSwkgPNCWCSBM1dwJOSAMzwkgHNBLCSAs0JYJaSAMzwkgHNAtCSAs0EsJIDzQ4QkgTNLuCSBc1dwJQBAgMEkgID3AAYkwHCwpMCwsKTA8LDkwTCwpMFwsKTBsLCkwfCw5MIwsKTCcPCkwrDwpMLw8OTDMPCkw3DwpMOwsKTD8LCkxPCwpMUwsKTFcLDkxbDwpMXwsKTGMLCkxnCwpMaw8KTG8LD";
        forcedMessageBuffer = Convert.FromBase64String(forcedMessage);
    }
    else
    {
        forcedMessage = new string('x', maxMessageSize + 1);
        forcedMessageBuffer = Encoding.UTF8.GetBytes(forcedMessage);
    }

    var receiveBuffer = new byte[maxMessageSize];

    var receiveResult = await webSocket.ReceiveAsync(
        new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

    while (!receiveResult.CloseStatus.HasValue)
    {
        var type = receiveResult.MessageType == WebSocketMessageType.Text ? "Txt" : "Bin";
        var message = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
        logger.LogInformation("[{SocketId}] Recv: [{MessageType}] {Message}", socketId, type, message);

        byte[] sendBuffer;
        string sendMessage;
        int sendMessageSize;
        WebSocketMessageType sendType;
        if (maxEchoesUntilForcedMessage >= 0 && echoesUntilForcedMessage >= maxEchoesUntilForcedMessage)
        {
            sendBuffer = forcedMessageBuffer;
            sendMessage = forcedMessage;
            sendMessageSize = forcedMessageBuffer.Length;
            sendType = forcedMessageType;
            echoesUntilForcedMessage = 0;
        }
        else
        {
            sendBuffer = receiveBuffer;
            sendMessage = message;
            sendMessageSize = receiveResult.Count;
            sendType = receiveResult.MessageType;
            ++echoesUntilForcedMessage;
        }

        var messageLogSize = Math.Min(sendMessageSize, maxMessageSize);
        logger.LogInformation("[{SocketId}] Send: [{MessageType}] {Message}",
            socketId, type, sendMessage[..messageLogSize]);

        await webSocket.SendAsync(
            new ArraySegment<byte>(sendBuffer, 0, sendMessageSize),
            sendType,
            true,
            CancellationToken.None);

        receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
    }

    await webSocket.CloseAsync(
        receiveResult.CloseStatus.Value,
        receiveResult.CloseStatusDescription,
        CancellationToken.None);
}
#endregion
