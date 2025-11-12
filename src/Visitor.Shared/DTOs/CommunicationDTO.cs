namespace Visitor.Shared.DTOs;

public class CommunicationDTO
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Company { get; set; }

    public string Mode { get; set; } = "UNKNOWN";
}
