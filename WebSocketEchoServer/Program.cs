using System.Net;
using System.Net.WebSockets;
using System.Text;

#region Main
Console.Title = "WebSocket Server";

var builder = WebApplication.CreateBuilder();

var app = builder.Build();
app.UseWebSockets();

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

        Console.WriteLine($"[{nextSocketId}] Conn: {context.Connection.Id}");

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

        Console.WriteLine($"[{nextSocketId}] DC: {context.Connection.Id}");
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});
#endregion

await app.RunAsync();
return;
#endregion

#region Helper Methods
async Task EchoAsync(ulong socketId, WebSocket webSocket)
{
    const int maxMessageSize = 1024 * 4;

    const int intentionalOverflowMaxEchoes = 4;
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
        Console.WriteLine($"[{socketId}] Recv: [{type}] {message}");

        byte[] sendBuffer;
        string sendMessage;
        int sendMessageSize;
        WebSocketMessageType sendType;
        if (intentionalOverflowCounter >= intentionalOverflowMaxEchoes)
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
        Console.WriteLine($"[{socketId}] Send: [{type}] {sendMessage[..1024]}");

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
