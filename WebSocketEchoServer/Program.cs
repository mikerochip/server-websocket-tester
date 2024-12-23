using System.Net;
using System.Net.WebSockets;
using System.Text;

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
var app = builder.Build();
app.UseWebSockets();
#endregion

#region Routes
app.Map("/hello", async context => await context.Response.WriteAsync("Hello!"));

ulong nextSocketId = 0;
app.Map("/", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        if (nextSocketId == ulong.MaxValue)
            nextSocketId = 1;
        else
            ++nextSocketId;

        logger.LogInformation("[{SocketId}] Conn: {ConnectionId}", nextSocketId, context.Connection.Id);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        try
        {
            await EchoAsync(nextSocketId, webSocket);
        }
        catch (WebSocketException e)
        {
            if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                throw;
        }

        logger.LogInformation("[{SocketId}] DC: {ConnectionId}", nextSocketId, context.Connection.Id);
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
    const int maxMessageLogSize = 1024;
    // make this less than 0 to stop intentional overflows
    const int intentionalOverflowMaxEchoes = -1;

    var intentionalOverflowCounter = 0;
    var overflowMessage = new string('x', maxMessageSize + 1);
    var overflowMessageBuffer = Encoding.UTF8.GetBytes(overflowMessage);

    var receiveBuffer = new byte[maxMessageSize];

    var receiveResult = await webSocket.ReceiveAsync(
        new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

    while (!receiveResult.CloseStatus.HasValue)
    {
        var type = receiveResult.MessageType == WebSocketMessageType.Text ? "Txt" : "Bin";
        var message = Encoding.UTF8.GetString(receiveBuffer);
        logger.LogInformation("[{SocketId}] Recv: [{MessageType}] {Message}", socketId, type, message);

        byte[] sendBuffer;
        string sendMessage;
        int sendMessageSize;
        WebSocketMessageType sendType;
        if (intentionalOverflowMaxEchoes >= 0 && intentionalOverflowCounter >= intentionalOverflowMaxEchoes)
        {
            sendBuffer = overflowMessageBuffer;
            sendMessage = overflowMessage;
            sendMessageSize = overflowMessage.Length;
            sendType = WebSocketMessageType.Text;
            intentionalOverflowCounter = 0;
        }
        else
        {
            sendBuffer = receiveBuffer;
            sendMessage = message;
            sendMessageSize = receiveResult.Count;
            sendType = receiveResult.MessageType;
            ++intentionalOverflowCounter;
        }
        logger.LogInformation("[{SocketId}] Send: [{MessageType}] {Message}",
            socketId, type, sendMessage[..maxMessageLogSize]);

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
