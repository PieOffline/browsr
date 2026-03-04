using System.Windows.Input;
using ProPilot.Helpers;
using ProPilot.Models;
using ProPilot.Services;

namespace ProPilot.ViewModels;

public class OnboardingViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly GeminiService _gemini;

    private int _currentStep;
    private string _firstName = string.Empty;
    private string _email = string.Empty;
    private string _school = string.Empty;
    private string _apiKey = string.Empty;
    private string _testResult = string.Empty;
    private bool _isTesting;

    public int CurrentStep { get => _currentStep; set => SetProperty(ref _currentStep, value); }
    public string FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }
    public string Email
    {
        get => _email;
        set
        {
            SetProperty(ref _email, value);
            AutoFillSchool();
        }
    }
    public string School { get => _school; set => SetProperty(ref _school, value); }
    public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
    public string TestResult { get => _testResult; set => SetProperty(ref _testResult, value); }
    public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }

    public ICommand NextStepCommand { get; }
    public ICommand PreviousStepCommand { get; }
    public ICommand TestKeyCommand { get; }
    public ICommand FinishCommand { get; }

    public event Action? OnboardingCompleted;

    public OnboardingViewModel(DatabaseService db, GeminiService gemini)
    {
        _db = db;
        _gemini = gemini;
        _currentStep = 0;

        NextStepCommand = new RelayCommand(_ => CurrentStep++);
        PreviousStepCommand = new RelayCommand(_ => { if (CurrentStep > 0) CurrentStep--; });
        TestKeyCommand = new RelayCommand(async _ => await TestApiKey());
        FinishCommand = new RelayCommand(async _ => await Finish());
    }

    private void AutoFillSchool()
    {
        if (_email.Contains("@fitzalan.cardiff.sch.uk"))
            School = "Fitzalan High School";
    }

    private async Task TestApiKey()
    {
        IsTesting = true;
        TestResult = "Testing...";
        var (success, message) = await _gemini.TestApiKeyAsync(ApiKey);
        TestResult = success ? "✅ " + message : "❌ " + message;
        IsTesting = false;
    }

    private async Task Finish()
    {
        await Task.Run(() =>
        {
            _db.SaveProfile(new Profile
            {
                Name = FirstName,
                Email = Email,
                School = School,
                GeminiApiKey = ApiKey
            });
        });
        _gemini.SetApiKey(ApiKey);
        OnboardingCompleted?.Invoke();
    }
}
