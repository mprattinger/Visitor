using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Visitor.Shared.Common.Interfaces;

namespace Visitor.Web.Infrastructure.Communication.Services;

public class VisitCommunicationService(NavigationManager navigationManager) : IVisitCommunicationService, IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event Action<string>? OnMessageReceived;

    public async Task CreateHub()
    {
        if (_hubConnection != null)
            return;

        var hubUrl = navigationManager.ToAbsoluteUri("/visithub");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
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

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
