using System.Text.Json;

namespace GateFlow.Domain.Entities;

public class SiteSettings
{
    public bool AllowManualOpen { get; set; } = true;
    public bool AllowRemoteOpen { get; set; } = true;
    public int VisitorDefaultMaxUses { get; set; } = 2;
    public int VisitorDefaultValidHours { get; set; } = 24;
    public bool RequireActiveVehicle { get; set; } = true;
    public bool DenyExpiredCredentials { get; set; } = true;
    public bool DenyBlacklistedVehicles { get; set; } = true;
    public bool LogDeniedAttempts { get; set; } = true;
    public bool AntiPassbackEnabled { get; set; }
    public int HardwareOfflineSeconds { get; set; } = 60;
    public Dictionary<string, bool> Features { get; set; } = new()
    {
        ["rfid"] = true,
        ["qr"] = true,
        ["barcode"] = true,
        ["anpr"] = false,
        ["mockGate"] = true,
    };

    public static SiteSettings Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new SiteSettings();
        try
        {
            var parsed = JsonSerializer.Deserialize<SiteSettings>(raw, JsonOpts);
            if (parsed is null) return new SiteSettings();
            parsed.Features ??= new Dictionary<string, bool>();
            foreach (var (k, v) in new SiteSettings().Features)
            {
                parsed.Features.TryAdd(k, v);
            }
            return parsed;
        }
        catch
        {
            return new SiteSettings();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}
