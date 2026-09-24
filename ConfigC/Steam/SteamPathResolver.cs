using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ConfigC.Steam;

/// <summary>Locates the local Steam installation across Windows, Linux and macOS.</summary>
public static class SteamPathResolver
{
    /// <summary>Returns every plausible Steam install directory for the current OS, most likely first.</summary>
    public static IEnumerable<string> GetCandidateSteamRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            var fromRegistry = TryReadWindowsRegistryPath();
            if (fromRegistry is not null)
                yield return fromRegistry;

            yield return @"C:\Program Files (x86)\Steam";
            yield return @"C:\Program Files\Steam";
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, "Library", "Application Support", "Steam");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".local", "share", "Steam");
            yield return Path.Combine(home, ".steam", "steam");
            yield return Path.Combine(home, ".steam", "root");
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam");
        }
    }

    /// <summary>Tries to auto-detect the Steam "userdata" directory, falling back to null when nothing is found.</summary>
    public static string? FindUserDataPath()
    {
        foreach (var root in GetCandidateSteamRoots())
        {
            var candidate = Path.Combine(root, "userdata");
            if (Directory.Exists(candidate))
                return candidate;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string? TryReadWindowsRegistryPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            return key?.GetValue("SteamPath") as string;
        }
        catch
        {
            return null;
        }
    }
}
