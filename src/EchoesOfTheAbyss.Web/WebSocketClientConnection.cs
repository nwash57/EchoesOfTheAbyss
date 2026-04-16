using System.Net.WebSockets;
using System.Text;
using EchoesOfTheAbyss.Lib.Game;

namespace EchoesOfTheAbyss.Web;

public class WebSocketClientConnection : IClientConnection
{
    private readonly WebSocket _webSocket;

    public WebSocketClientConnection(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public async Task SendAsync(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
