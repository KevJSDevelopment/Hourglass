using System.Net;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AppLimiter;
using AppLimiterLibrary.Dtos;

public class WebSocketServerService : BackgroundService, IWebSocketCommunicator
{
    private readonly ILogger<WebSocketServerService> _logger;
    private readonly WebsiteTracker _websiteTracker;
    private readonly CancellationTokenSource _serverCts;
    private readonly HttpListener _httpListener;
    private readonly List<WebSocket> _connectedClients;

    public WebSocketServerService(ILogger<WebSocketServerService> logger, WebsiteTracker websiteTracker)
    {
        _logger = logger;
        _websiteTracker = websiteTracker;
        _serverCts = new CancellationTokenSource();
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://localhost:5095/");
        _connectedClients = new List<WebSocket>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _httpListener.Start();
            _logger.LogInformation("WebSocket server started on port 5095");

            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    ProcessWebSocketRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket server");
        }
        finally
        {
            _httpListener.Stop();
            _serverCts.Cancel();
            
            foreach (var client in _connectedClients.ToList())
            {
                try
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing WebSocket connection");
                }
            }
        }
    }

    private async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        try
        {
            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket webSocket = webSocketContext.WebSocket;
            
            _connectedClients.Add(webSocket);
            _logger.LogInformation("New WebSocket connection established");

            try
            {
                await HandleWebSocketConnection(webSocket);
            }
            finally
            {
                _connectedClients.Remove(webSocket);
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket request");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    private async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        var buffer = new byte[4096];
        
        try
        {
            while (webSocket.State == WebSocketState.Open && !_serverCts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _serverCts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Connection closed by client", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessWebSocketMessage(message);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error handling WebSocket connection");
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, 
                    "Server error", CancellationToken.None);
            }
        }
    }

    private async Task ProcessWebSocketMessage(string message)
    {
        try
        {
            _logger.LogInformation("Received WebSocket message: {Message}", message);
            var update = JsonSerializer.Deserialize<TabUpdate>(message);
            _logger.LogInformation("Deserialized update - Type: {Type}, URL Count: {Count}",
                update?.Type, update?.Urls?.Count);

            if (update?.Type == "tabUpdate")
            {
                _websiteTracker.UpdateUrls(update.Urls);
                _logger.LogInformation("Updated active URLs in WebsiteTracker");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing message: {Message}", message);
        }
    }

    public async Task SendCloseTabCommand(string domain)
    {
        var message = JsonSerializer.Serialize(new
        {
            type = "closeTab",
            domain = domain
        });

        foreach (var client in _connectedClients)
        {
            if (client.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _serverCts.Cancel();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _serverCts.Dispose();
        _httpListener.Close();
        base.Dispose();
    }
}
