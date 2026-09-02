namespace GateFlow.Contracts.Health;

public sealed record HealthResponse(string Status, bool Ok, string Service, string Runtime, DateTime Utc, string? Database);
