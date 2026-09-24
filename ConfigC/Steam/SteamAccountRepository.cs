using System.Text.RegularExpressions;

namespace ConfigC.Steam;

/// <summary>Reads the Steam profiles present under a "userdata" directory, enriched with persona names.</summary>
public static partial class SteamAccountRepository
{
    public static IReadOnlyList<SteamAccount> GetAccounts(string userDataPath)
    {
        var personaNames = ReadPersonaNames(userDataPath);

        return Directory.GetDirectories(userDataPath)
            .Select(folder =>
            {
                var steamId = Path.GetFileName(folder);
                personaNames.TryGetValue(steamId, out var personaName);
                return new SteamAccount(steamId, folder, personaName);
            })
            .OrderBy(account => account.PersonaName ?? account.SteamId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Best-effort parse of Steam's "loginusers.vdf" to map SteamID64 -> persona (display) name.
    /// Returns an empty map when the file is missing or unreadable; the tool still works without it.
    /// </summary>
    private static Dictionary<string, string> ReadPersonaNames(string userDataPath)
    {
        var result = new Dictionary<string, string>();

        // userdata's parent is the Steam install root, where config/loginusers.vdf lives.
        var steamRoot = Directory.GetParent(userDataPath)?.FullName;
        if (steamRoot is null)
            return result;

        var vdfPath = Path.Combine(steamRoot, "config", "loginusers.vdf");
        if (!File.Exists(vdfPath))
            return result;

        try
        {
            string? currentSteamId = null;
            foreach (var rawLine in File.ReadLines(vdfPath))
            {
                var line = rawLine.Trim();

                var idMatch = SteamIdLine().Match(line);
                if (idMatch.Success)
                {
                    currentSteamId = idMatch.Groups[1].Value;
                    continue;
                }

                var nameMatch = PersonaNameLine().Match(line);
                if (nameMatch.Success && currentSteamId is not null)
                {
                    result[currentSteamId] = nameMatch.Groups[1].Value;
                }
            }
        }
        catch
        {
            // Corrupt or locked file: fall back to showing raw SteamIDs.
        }

        return result;
    }

    [GeneratedRegex(@"^""(\d{17})""$")]
    private static partial Regex SteamIdLine();

    [GeneratedRegex(@"^""PersonaName""\s*""([^""]*)""$")]
    private static partial Regex PersonaNameLine();
}
