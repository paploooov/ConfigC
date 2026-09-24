namespace ConfigC.Steam;

/// <summary>A single Steam profile found under the local "userdata" directory.</summary>
public sealed record SteamAccount(string SteamId, string FolderPath, string? PersonaName)
{
    public string DisplayName => PersonaName is { Length: > 0 } ? $"{PersonaName} ({SteamId})" : SteamId;
}
