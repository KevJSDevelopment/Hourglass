// Hourglass/WebSocketServerService.cs
using HourglassLibrary.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hourglass
{
    public class WebSocketServerService : IWebSocketCommunicator, IHostedService
    {
        private readonly ILogger<WebSocketServerService> _logger;
        private readonly IWebsiteTracker _websiteTracker; // To update URLs
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private const string WebSocketPath = "/ws";

        public WebSocketServerService(ILogger<WebSocketServerService> logger, IWebsiteTracker websiteTracker)
        {
            _logger = logger;
            _websiteTracker = websiteTracker;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8080/"); // Configure via appsettings.json later
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            _logger.LogInformation("WebSocket server started at {Url}", "http://localhost:8080/");
            _ = Task.Run(() => AcceptWebSocketClientsAsync(_cts.Token));
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            _listener.Stop();
            _logger.LogInformation("WebSocket server stopped");
            await Task.CompletedTask;
        }

        private async Task AcceptWebSocketClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        _logger.LogInformation("WebSocket connection established");
                        _ = HandleWebSocketAsync(wsContext.WebSocket, cancellationToken);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting WebSocket client");
                }
            }
        }

        private async Task HandleWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogDebug("Received message: {Message}", message);
                        // Assuming message is a JSON list of URLs, e.g., ["http://example.com", ...]
                        var urls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(message);
                        _websiteTracker.UpdateUrls(urls);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection");
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed)
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Unexpected closure", CancellationToken.None);
                webSocket.Dispose();
            }
        }


        public async Task SendCloseTabCommand(string domain)
        {
            _logger.LogInformation("Sending close tab command for domain: {Domain}", domain);
            // Implement the logic to send the command via WebSocket
            await Task.CompletedTask; // Placeholder
        }
    }
}