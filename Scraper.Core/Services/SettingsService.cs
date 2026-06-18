using System;
using System.IO;
using System.Text.Json;
using Scraper.Core.Models;

namespace Scraper.Core.Services;

public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "WebScraperPro"
    );
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public static ScraperSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<ScraperSettings>(json) ?? GetDefaultSettings();
            }
        }
        catch
        {
            // Fallback to default on read error
        }
        
        return GetDefaultSettings();
    }

    private static ScraperSettings GetDefaultSettings()
    {
        var defaultSettings = new ScraperSettings();
        try
        {
            defaultSettings.OutputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "WebScraperPro"
            );
        }
        catch
        {
            defaultSettings.OutputFolder = "C:\\WebScraperPro";
        }
        return defaultSettings;
    }

    public static void Save(ScraperSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
        }
    }
}
