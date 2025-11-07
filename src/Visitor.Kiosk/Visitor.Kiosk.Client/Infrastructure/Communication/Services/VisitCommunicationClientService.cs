using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Visitor.Shared.Common.Interfaces;

namespace Visitor.Kiosk.Client.Infrastructure.Communication.Services;

public class VisitCommunicationClientService(NavigationManager navigationManager, IConfiguration configuration) : IVisitCommunicationService, IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event Action<string>? OnMessageReceived;

    public async Task CreateHub()
    {
        if (_hubConnection != null)
            return;

        var serverUrl = configuration["ServerUrlHttps"] ?? string.Empty;
        serverUrl = serverUrl.TrimEnd('/') + "/visithub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri(serverUrl)) // Use full server URL for WASM
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("ReceiveMessage", (message) =>
        {
            OnMessageReceived?.Invoke(message);
        });

        await _hubConnection.StartAsync();
    }

    public async Task SendMessage(string message)
    {
        if (_hubConnection == null)
            throw new InvalidOperationException("Connection not started.");

        await _hubConnection.SendAsync("SendMessage", message);
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}
