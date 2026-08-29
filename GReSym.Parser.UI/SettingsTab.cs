using Gtk;
using System;
using System.IO;
using System.Text.Json;

namespace GReSym.Parser.UI;

public class SettingsTab : IParserTab
{
    private readonly Builder _builder;
    private readonly string _settingsFile = "user_settings.json";

    private RadioButton _fullModeRadio;
    private RadioButton _incrementalModeRadio;
    private Entry _batchSizeEntry;
    private Entry _threadsCountEntry;
    private Entry _steamApiEntry;

    private UserSettings _currentSettings;

    public SettingsTab(Builder builder)
    {
        _builder = builder;
        _currentSettings = LoadSettingsFromFile();
    }

    public Widget GetWidget() => (Widget)_builder.GetObject("settings_tab");

    public void Initialize()
    {
        // Получаем виджеты из Glade
        _fullModeRadio = (RadioButton)_builder.GetObject("parse_mode_full_radiobutton");
        _incrementalModeRadio = (RadioButton)_builder.GetObject("parse_mode_incremental_radiobutton");
        _batchSizeEntry = (Entry)_builder.GetObject("batch_size_entry");
        _threadsCountEntry = (Entry)_builder.GetObject("threads_count_entry");
        _steamApiEntry = (Entry)_builder.GetObject("steam_api_entry");

        // Подгружаем сохранённые значения
        _fullModeRadio.Active = _currentSettings.ParseModeFull;
        _incrementalModeRadio.Active = !_currentSettings.ParseModeFull;
        _batchSizeEntry.Text = _currentSettings.BatchSize.ToString();
        _threadsCountEntry.Text = _currentSettings.ThreadsCount.ToString();
        _steamApiEntry.Text = _currentSettings.SteamApiKey ?? string.Empty;

        // Подписываемся на изменения
        _fullModeRadio.Toggled += OnModeChanged;
        _incrementalModeRadio.Toggled += OnModeChanged;

        _batchSizeEntry.Changed += OnBatchSizeChanged;
        _threadsCountEntry.Changed += OnThreadsCountChanged;
        _steamApiEntry.Changed += OnApiKeyChanged;
    }

    private void OnModeChanged(object sender, EventArgs e)
    {
        _currentSettings.ParseModeFull = _fullModeRadio.Active;
        SaveSettingsToFile();
    }

    private void OnBatchSizeChanged(object sender, EventArgs e)
    {
        if (int.TryParse(_batchSizeEntry.Text, out int batch))
        {
            _currentSettings.BatchSize = batch;
            SaveSettingsToFile();
        }
    }

    private void OnThreadsCountChanged(object sender, EventArgs e)
    {
        if (int.TryParse(_threadsCountEntry.Text, out int threads))
        {
            _currentSettings.ThreadsCount = threads;
            SaveSettingsToFile();
        }
    }

    private void OnApiKeyChanged(object sender, EventArgs e)
    {
        _currentSettings.SteamApiKey = _steamApiEntry.Text;
        SaveSettingsToFile();
    }

    private void SaveSettingsToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
        }
    }

    private UserSettings LoadSettingsFromFile()
    {
        try
        {
            if (File.Exists(_settingsFile))
            {
                var json = File.ReadAllText(_settingsFile);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
        }

        // По умолчанию
        return new UserSettings();
    }
}

// Модель настроек
public class UserSettings
{
    public bool ParseModeFull { get; set; } = true;
    public int BatchSize { get; set; } = 10;
    public int ThreadsCount { get; set; } = 5;
    public string? SteamApiKey { get; set; }
}