using GateFlow.Contracts.Sites;
using GateFlow.Domain.Entities;

namespace GateFlow.Api.Mapping;

public static class SiteSettingsMapper
{
    public static SiteSettingsDto ToDto(SiteSettings settings) => new()
    {
        AllowManualOpen = settings.AllowManualOpen,
        AllowRemoteOpen = settings.AllowRemoteOpen,
        VisitorDefaultMaxUses = settings.VisitorDefaultMaxUses,
        VisitorDefaultValidHours = settings.VisitorDefaultValidHours,
        RequireActiveVehicle = settings.RequireActiveVehicle,
        DenyExpiredCredentials = settings.DenyExpiredCredentials,
        DenyBlacklistedVehicles = settings.DenyBlacklistedVehicles,
        LogDeniedAttempts = settings.LogDeniedAttempts,
        AntiPassbackEnabled = settings.AntiPassbackEnabled,
        HardwareOfflineSeconds = settings.HardwareOfflineSeconds,
        Features = new Dictionary<string, bool>(settings.Features),
    };

    public static SiteSettingsDto FromJson(string? raw) => ToDto(SiteSettings.Parse(raw));
}
