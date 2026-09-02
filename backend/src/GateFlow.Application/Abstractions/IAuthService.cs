using GateFlow.Contracts.Auth;

namespace GateFlow.Application.Abstractions;

public interface IAuthService
{
    Task<(bool Ok, string? Error, AuthResponse? Payload)> RegisterClientAsync(
        string companyName,
        string fullName,
        string email,
        string password,
        string? phone);

    Task<(bool Ok, string? Error, AuthResponse? Payload)> LoginAsync(string email, string password);
}
