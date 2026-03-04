using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using ProPilot.Helpers;
using ProPilot.Models;
using ProPilot.Services;

namespace ProPilot.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly GeminiService _gemini;

    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _school = string.Empty;
    private string _apiKey = string.Empty;
    private string _maskedKey = string.Empty;
    private string _testResult = string.Empty;
    private string _saveStatus = string.Empty;
    private bool _isTesting;
    private bool _showApiKey;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string School { get => _school; set => SetProperty(ref _school, value); }
    public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
    public string MaskedKey { get => _maskedKey; set => SetProperty(ref _maskedKey, value); }
    public string TestResult { get => _testResult; set => SetProperty(ref _testResult, value); }
    public string SaveStatus { get => _saveStatus; set => SetProperty(ref _saveStatus, value); }
    public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }
    public bool ShowApiKey { get => _showApiKey; set => SetProperty(ref _showApiKey, value); }
    public string DataFolderPath => _db.DataFolder;

    public ICommand SaveCommand { get; }
    public ICommand TestKeyCommand { get; }
    public ICommand ToggleKeyVisibilityCommand { get; }
    public ICommand ResetAllDataCommand { get; }
    public ICommand OpenDataFolderCommand { get; }

    public SettingsViewModel(DatabaseService db, GeminiService gemini)
    {
        _db = db;
        _gemini = gemini;

        SaveCommand = new RelayCommand(_ => SaveProfile());
        TestKeyCommand = new RelayCommand(async _ => await TestKey());
        ToggleKeyVisibilityCommand = new RelayCommand(_ => ShowApiKey = !ShowApiKey);
        ResetAllDataCommand = new RelayCommand(_ => { }); // Confirmation handled in view
        OpenDataFolderCommand = new RelayCommand(_ => OpenDataFolder());

        LoadProfile();
    }

    public void LoadProfile()
    {
        var profile = _db.GetProfile();
        if (profile == null) return;

        Name = profile.Name;
        Email = profile.Email;
        School = profile.School;
        ApiKey = profile.GeminiApiKey;
        MaskKey();
    }

    private void MaskKey()
    {
        if (ApiKey.Length > 8)
            MaskedKey = ApiKey[..4] + new string('•', ApiKey.Length - 8) + ApiKey[^4..];
        else
            MaskedKey = new string('•', ApiKey.Length);
    }

    private void SaveProfile()
    {
        _db.SaveProfile(new Profile
        {
            Name = Name,
            Email = Email,
            School = School,
            GeminiApiKey = ApiKey
        });
        _gemini.SetApiKey(ApiKey);
        MaskKey();
        SaveStatus = "✅ Profile saved!";
    }

    private async Task TestKey()
    {
        IsTesting = true;
        TestResult = "Testing...";
        var (success, message) = await _gemini.TestApiKeyAsync(ApiKey);
        TestResult = success ? "✅ " + message : "❌ " + message;
        IsTesting = false;
    }

    private void OpenDataFolder()
    {
        try
        {
            if (Directory.Exists(DataFolderPath))
                Process.Start(new ProcessStartInfo(DataFolderPath) { UseShellExecute = true });
        }
        catch { }
    }

    public void ResetAllData()
    {
        _db.ResetAllData();
        Name = string.Empty;
        Email = string.Empty;
        School = string.Empty;
        ApiKey = string.Empty;
        MaskedKey = string.Empty;
        SaveStatus = "All data has been reset.";
    }
}
