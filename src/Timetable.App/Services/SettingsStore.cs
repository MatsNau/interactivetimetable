using System.IO;
using System.Text.Json;

namespace Timetable.App.Services;

/// <summary>Lokale Benutzereinstellungen (%AppData%\InteractiveTimetable\settings.json).</summary>
public sealed class SettingsStore
{
    private sealed record Settings(string? LastPlanPath);

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "InteractiveTimetable", "settings.json");

    public string? LastPlanPath { get; set; }

    public SettingsStore()
    {
        try
        {
            if (File.Exists(SettingsPath))
                LastPlanPath = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath))?.LastPlanPath;
        }
        catch (Exception)
        {
            // Defekte oder unlesbare Einstellungen: mit Standardwerten starten.
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new Settings(LastPlanPath)));
        }
        catch (Exception)
        {
            // Einstellungen sind nice-to-have; ein Fehlschlag darf die App nicht stören.
        }
    }
}
