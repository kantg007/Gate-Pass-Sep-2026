using GateFlow.Contracts.Access;

namespace GateFlow.Application.Abstractions;

public interface IAccessService
{
    Task<AccessCheckResponse> CheckAsync(
        AccessCheckRequest request,
        string? deviceApiKey,
        CancellationToken ct = default);
}
