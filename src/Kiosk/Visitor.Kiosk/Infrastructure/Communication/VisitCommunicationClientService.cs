using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Visitor.Kiosk.Common.Interfaces;
using Visitor.Kiosk.Features.CheckIn;
using Visitor.Shared.DTOs;

namespace Visitor.Kiosk.Infrastructure.Communication;

public class VisitCommunicationClientService(NavigationManager navigationManager, IConfiguration configuration) : IVisitCommunicationClientService
{
    private HubConnection? _hubConnection;

    public event Action<CommunicationDTO>? OnMessageReceived;

    public async Task CreateHub()
    {
        if (_hubConnection != null)
            return;

        var serverUrl = configuration["SignalRHub"] ?? "https://localhost:7002";
        serverUrl = serverUrl.TrimEnd('/') + "/visithub";
        Console.WriteLine(serverUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri(serverUrl)) // Use full server URL for WASM
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("Broadcast", (message) =>
        {
            var dto = JsonSerializer.Deserialize<CommunicationDTO>(message);
            if (dto == null)
            {
                throw new ArgumentException("Invalid payload");
            }
            OnMessageReceived?.Invoke(dto);
        });

        await _hubConnection.StartAsync();

        await _hubConnection.SendAsync("Hello", $"kiosk;{_hubConnection.ConnectionId};");
    }

    public Task SendMessage(string message)
    {
        throw new NotImplementedException();
    }

    public async Task SendVisitorCheckIn(Guid id, string name, string company, CheckinMode mode)
    {
        if (_hubConnection is null)
        {
            await CreateHub();
        }

        var modeString = mode switch
        {
            CheckinMode.UNKNOWN => "UNKNOWN",
            CheckinMode.SELF => "SELF_CHECK_IN",
            CheckinMode.REMOTE => "REMOTE_CHECK_IN",
            _ => "UNKNOWN"
        };

        var dto = new CommunicationDTO
        {
            Id = id,
            Name = name,
            Company = company,
            Mode = modeString
        };
        var payload = JsonSerializer.Serialize(dto);

        await _hubConnection!.SendAsync("VisitorCheckIn", payload);
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
