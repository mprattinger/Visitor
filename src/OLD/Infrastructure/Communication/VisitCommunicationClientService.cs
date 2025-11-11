using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Visitor.Kiosk.Common.Interfaces;

namespace Visitor.Kiosk.Infrastructure.Communication;

public class VisitCommunicationClientService(IConfiguration configuration, NavigationManager navigationManager) : IVisitCommunicationClientService
{
    private HubConnection? _hubConnection;

    public event Action<string>? OnMessageReceived;

    public async Task CreateHub()
    {
        if (_hubConnection != null)
            return;

        var serverUrl = configuration["ServerUrlHttps"] ?? "https://localhost:7002";
        serverUrl = serverUrl.TrimEnd('/') + "/visithub";
        Console.WriteLine(serverUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri(serverUrl)) // Use full server URL for WASM
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("Broadcast", (message) =>
        {
            OnMessageReceived?.Invoke(message);
        });

        await _hubConnection.StartAsync();

        await _hubConnection.SendAsync("Hello", $"kiosk;{_hubConnection.ConnectionId};");
    }

    public async Task SendMessage(string message)
    {
        if (_hubConnection == null)
            throw new InvalidOperationException("Connection not started.");

        await _hubConnection.SendAsync("SendMessage", message);
    }

    public async Task SendVisitorCheckIn(string name, string company)
    {
        if (_hubConnection == null)
            throw new InvalidOperationException("Connection not started.");

        await _hubConnection.SendAsync("VisitorCheckIn", name, company);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}
