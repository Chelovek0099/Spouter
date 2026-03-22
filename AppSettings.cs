using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Soundpad;

public sealed class PadSettings
{
    public string? FilePath { get; set; }
    public float Volume { get; set; }
    public string Hotkey { get; set; } = nameof(Key.F1);
    public string HotkeyModifiers { get; set; } = nameof(ModifierKeys.None);
}

public sealed class AppSettings
{
    public string? OutputDeviceId { get; set; }
    public List<PadSettings> Pads { get; set; } = new();
}

public static class AppSettingsStore
{
    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "soundpad.settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // намеренно игнорируем ошибки записи настроек
        }
    }
}

