using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using ProPilot.Helpers;
using ProPilot.Models;
using ProPilot.Services;

namespace ProPilot.ViewModels;

public class AssignmentsViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly GeminiService _gemini;

    private Assignment? _selectedAssignment;
    private bool _showNewForm;
    private bool _showDetail;
    private bool _isAnalysing;
    private byte[]? _screenshotData;
    private string _analyseStatus = string.Empty;

    // New assignment form fields
    private string _newTitle = string.Empty;
    private string _newSubject = string.Empty;
    private string _newClassName = string.Empty;
    private string _newDescription = string.Empty;
    private string _newBrief = string.Empty;
    private string _newDeadline = string.Empty;

    public ObservableCollection<Assignment> Assignments { get; } = new();

    public Assignment? SelectedAssignment
    {
        get => _selectedAssignment;
        set { SetProperty(ref _selectedAssignment, value); ShowDetail = value != null && !ShowNewForm; }
    }

    public bool ShowNewForm { get => _showNewForm; set => SetProperty(ref _showNewForm, value); }
    public bool ShowDetail { get => _showDetail; set => SetProperty(ref _showDetail, value); }
    public bool IsAnalysing { get => _isAnalysing; set => SetProperty(ref _isAnalysing, value); }
    public byte[]? ScreenshotData { get => _screenshotData; set => SetProperty(ref _screenshotData, value); }
    public string AnalyseStatus { get => _analyseStatus; set => SetProperty(ref _analyseStatus, value); }

    public string NewTitle { get => _newTitle; set => SetProperty(ref _newTitle, value); }
    public string NewSubject { get => _newSubject; set => SetProperty(ref _newSubject, value); }
    public string NewClassName { get => _newClassName; set => SetProperty(ref _newClassName, value); }
    public string NewDescription { get => _newDescription; set => SetProperty(ref _newDescription, value); }
    public string NewBrief { get => _newBrief; set => SetProperty(ref _newBrief, value); }
    public string NewDeadline { get => _newDeadline; set => SetProperty(ref _newDeadline, value); }

    public ICommand ShowNewFormCommand { get; }
    public ICommand CloseFormCommand { get; }
    public ICommand AnalyseScreenshotCommand { get; }
    public ICommand SaveAssignmentCommand { get; }
    public ICommand DeleteAssignmentCommand { get; }
    public ICommand BackToListCommand { get; }

    public AssignmentsViewModel(DatabaseService db, GeminiService gemini)
    {
        _db = db;
        _gemini = gemini;

        ShowNewFormCommand = new RelayCommand(_ => { ClearForm(); ShowNewForm = true; ShowDetail = false; });
        CloseFormCommand = new RelayCommand(_ => { ShowNewForm = false; });
        AnalyseScreenshotCommand = new RelayCommand(async _ => await AnalyseScreenshot(), _ => ScreenshotData != null && !IsAnalysing);
        SaveAssignmentCommand = new RelayCommand(_ => SaveAssignment());
        DeleteAssignmentCommand = new RelayCommand(_ => DeleteSelectedAssignment());
        BackToListCommand = new RelayCommand(_ => { ShowDetail = false; SelectedAssignment = null; });

        LoadAssignments();
    }

    public void LoadAssignments()
    {
        Assignments.Clear();
        foreach (var a in _db.GetAssignments())
            Assignments.Add(a);
    }

    public void SetScreenshotFromClipboard(byte[] imageData)
    {
        ScreenshotData = imageData;
        AnalyseStatus = "Screenshot loaded. Click Analyse to process.";
    }

    private async Task AnalyseScreenshot()
    {
        if (ScreenshotData == null) return;
        IsAnalysing = true;
        AnalyseStatus = "Analysing screenshot with AI...";

        var prompt = @"Analyse this assignment screenshot and return ONLY a valid JSON object with no markdown or extra text, in this exact format:
{
  ""title"": ""string"",
  ""subject"": ""string"",
  ""class_name"": ""string"",
  ""description"": ""string"",
  ""brief"": ""string"",
  ""deadline"": ""string or null""
}";

        var result = await _gemini.SendImageAsync(prompt, ScreenshotData);

        try
        {
            // Try to parse JSON from response (handle possible markdown wrapping)
            var json = result;
            if (json.Contains("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start)
                    json = json.Substring(start, end - start + 1);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            NewTitle = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            NewSubject = root.TryGetProperty("subject", out var s) ? s.GetString() ?? "" : "";
            NewClassName = root.TryGetProperty("class_name", out var c) ? c.GetString() ?? "" : "";
            NewDescription = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            NewBrief = root.TryGetProperty("brief", out var b) ? b.GetString() ?? "" : "";
            NewDeadline = root.TryGetProperty("deadline", out var dl) && dl.ValueKind != JsonValueKind.Null ? dl.GetString() ?? "" : "";

            AnalyseStatus = "✅ Analysis complete! Review the fields below.";
        }
        catch
        {
            AnalyseStatus = "⚠️ Could not parse AI response. Please fill in fields manually.";
        }

        IsAnalysing = false;
    }

    private void SaveAssignment()
    {
        var assignment = new Assignment
        {
            Title = NewTitle,
            Subject = NewSubject,
            ClassName = NewClassName,
            Description = NewDescription,
            Brief = NewBrief,
            Deadline = NewDeadline
        };

        _db.CreateAssignment(assignment);
        ShowNewForm = false;
        ClearForm();
        LoadAssignments();
    }

    private void DeleteSelectedAssignment()
    {
        if (SelectedAssignment == null) return;
        _db.DeleteAssignment(SelectedAssignment.Id);
        ShowDetail = false;
        SelectedAssignment = null;
        LoadAssignments();
    }

    private void ClearForm()
    {
        NewTitle = string.Empty;
        NewSubject = string.Empty;
        NewClassName = string.Empty;
        NewDescription = string.Empty;
        NewBrief = string.Empty;
        NewDeadline = string.Empty;
        ScreenshotData = null;
        AnalyseStatus = string.Empty;
    }
}
