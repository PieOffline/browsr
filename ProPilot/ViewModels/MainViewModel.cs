using System.Windows.Input;
using ProPilot.Helpers;
using ProPilot.Services;

namespace ProPilot.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly GeminiService _gemini;
    private readonly DocumentService _docService;

    private ViewModelBase? _currentView;
    private bool _showOnboarding;
    private string _currentViewTitle = "Chat";
    private bool _isChatActive;
    private bool _isAssignmentsActive;
    private bool _isDocumentsActive;
    private bool _isSettingsActive;

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set { SetProperty(ref _currentView, value); UpdateTitle(); }
    }

    public bool ShowOnboarding
    {
        get => _showOnboarding;
        set => SetProperty(ref _showOnboarding, value);
    }

    public string CurrentViewTitle
    {
        get => _currentViewTitle;
        set => SetProperty(ref _currentViewTitle, value);
    }

    public bool IsChatActive { get => _isChatActive; set => SetProperty(ref _isChatActive, value); }
    public bool IsAssignmentsActive { get => _isAssignmentsActive; set => SetProperty(ref _isAssignmentsActive, value); }
    public bool IsDocumentsActive { get => _isDocumentsActive; set => SetProperty(ref _isDocumentsActive, value); }
    public bool IsSettingsActive { get => _isSettingsActive; set => SetProperty(ref _isSettingsActive, value); }

    public ChatViewModel ChatVm { get; }
    public AssignmentsViewModel AssignmentsVm { get; }
    public DocumentsViewModel DocumentsVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public OnboardingViewModel OnboardingVm { get; }

    public ICommand NavigateChatCommand { get; }
    public ICommand NavigateAssignmentsCommand { get; }
    public ICommand NavigateDocumentsCommand { get; }
    public ICommand NavigateSettingsCommand { get; }

    public MainViewModel()
    {
        _db = new DatabaseService();
        _gemini = new GeminiService();
        _docService = new DocumentService();

        ChatVm = new ChatViewModel(_db, _gemini, _docService);
        AssignmentsVm = new AssignmentsViewModel(_db, _gemini);
        DocumentsVm = new DocumentsViewModel(_db, _docService);
        SettingsVm = new SettingsViewModel(_db, _gemini);
        OnboardingVm = new OnboardingViewModel(_db, _gemini);

        NavigateChatCommand = new RelayCommand(_ => { CurrentView = ChatVm; SetActiveTab(nameof(IsChatActive)); });
        NavigateAssignmentsCommand = new RelayCommand(_ => { CurrentView = AssignmentsVm; SetActiveTab(nameof(IsAssignmentsActive)); });
        NavigateDocumentsCommand = new RelayCommand(_ => { CurrentView = DocumentsVm; SetActiveTab(nameof(IsDocumentsActive)); });
        NavigateSettingsCommand = new RelayCommand(_ => { CurrentView = SettingsVm; SetActiveTab(nameof(IsSettingsActive)); });

        OnboardingVm.OnboardingCompleted += () =>
        {
            ShowOnboarding = false;
            LoadApiKey();
            CurrentView = ChatVm;
            SetActiveTab(nameof(IsChatActive));
        };

        // Check if profile exists
        var profile = _db.GetProfile();
        if (profile == null)
        {
            ShowOnboarding = true;
        }
        else
        {
            ShowOnboarding = false;
            _gemini.SetApiKey(profile.GeminiApiKey);
            CurrentView = ChatVm;
            SetActiveTab(nameof(IsChatActive));
        }
    }

    private void SetActiveTab(string activeProperty)
    {
        IsChatActive = activeProperty == nameof(IsChatActive);
        IsAssignmentsActive = activeProperty == nameof(IsAssignmentsActive);
        IsDocumentsActive = activeProperty == nameof(IsDocumentsActive);
        IsSettingsActive = activeProperty == nameof(IsSettingsActive);
    }

    private void LoadApiKey()
    {
        var profile = _db.GetProfile();
        if (profile != null)
            _gemini.SetApiKey(profile.GeminiApiKey);
    }

    private void UpdateTitle()
    {
        CurrentViewTitle = CurrentView switch
        {
            ChatViewModel => "Chat",
            AssignmentsViewModel => "Assignments",
            DocumentsViewModel => "Documents",
            SettingsViewModel => "Settings",
            _ => "ProPilot"
        };
    }
}
