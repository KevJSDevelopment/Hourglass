using HourglassLibrary.Interfaces;
using HourglassLibrary.Dtos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hourglass
{
    public class WebSocketServerService : IWebSocketCommunicator, IHostedService
    {
        private readonly ILogger<WebSocketServerService> _logger;
        private readonly IWebsiteTracker _websiteTracker;
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<WebSocket> _clients = new();
        private readonly object _clientsLock = new object();

        public WebSocketServerService(ILogger<WebSocketServerService> logger, IWebsiteTracker websiteTracker)
        {
            _logger = logger;
            _websiteTracker = websiteTracker;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5095/websocket/tabs/");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            _logger.LogInformation("WebSocket server started at http://localhost:5095/websocket/tabs/");
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
                        lock (_clientsLock)
                        {
                            _clients.Add(wsContext.WebSocket);
                        }
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
                        try
                        {
                            var update = JsonSerializer.Deserialize<TabUpdate>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            _logger.LogDebug("Deserialized update - Type: {Type}, URL Count: {Count}", update?.Type, update?.Urls?.Count);
                            if (update?.Type == "tabUpdate" && update.Urls != null)
                            {
                                _websiteTracker.UpdateUrls(update.Urls);
                                _logger.LogDebug("Updated active URLs in WebsiteTracker");
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Error deserializing message: {Message}", message);
                        }
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
                lock (_clientsLock)
                {
                    _clients.Remove(webSocket);
                }
                if (webSocket.State != WebSocketState.Closed)
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Unexpected closure", CancellationToken.None);
                webSocket.Dispose();
            }
        }

        public async Task SendCloseTabCommand(string domain)
        {
            _logger.LogInformation("Sending close tab command for domain: {Domain}", domain);
            var message = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { type = "closeTab", domain })
            );

            // Collect open clients inside the lock
            List<WebSocket> openClients;
            lock (_clientsLock)
            {
                openClients = _clients.Where(c => c.State == WebSocketState.Open).ToList();
            }

            // Send messages outside the lock
            foreach (var client in openClients)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
                    _logger.LogDebug("Sent close tab command to client for domain: {Domain}", domain);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending close tab command to client for domain: {Domain}", domain);
                }
            }
        }
    }
}