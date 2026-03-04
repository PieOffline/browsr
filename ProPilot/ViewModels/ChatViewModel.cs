using System.Collections.ObjectModel;
using System.Windows.Input;
using ProPilot.Helpers;
using ProPilot.Models;
using ProPilot.Services;

namespace ProPilot.ViewModels;

public class ChatViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly GeminiService _gemini;
    private readonly DocumentService _docService;

    private string _messageText = string.Empty;
    private ChatSession? _currentSession;
    private bool _isSending;
    private Assignment? _selectedAssignment;
    private Document? _attachedDocument;
    private bool _showSessionList = true;
    private ObservableCollection<Assignment> _linkedAssignments = new();
    private ObservableCollection<Document> _attachedDocuments = new();

    public ObservableCollection<ChatSession> Sessions { get; } = new();
    public ObservableCollection<Message> Messages { get; } = new();
    public ObservableCollection<Assignment> AvailableAssignments { get; } = new();

    public string MessageText { get => _messageText; set => SetProperty(ref _messageText, value); }
    public ChatSession? CurrentSession
    {
        get => _currentSession;
        set
        {
            SetProperty(ref _currentSession, value);
            if (value != null) LoadMessages();
        }
    }
    public bool IsSending { get => _isSending; set => SetProperty(ref _isSending, value); }
    public Assignment? SelectedAssignment { get => _selectedAssignment; set => SetProperty(ref _selectedAssignment, value); }
    public Document? AttachedDocument { get => _attachedDocument; set => SetProperty(ref _attachedDocument, value); }
    public bool ShowSessionList { get => _showSessionList; set => SetProperty(ref _showSessionList, value); }
    public ObservableCollection<Assignment> LinkedAssignments { get => _linkedAssignments; set => SetProperty(ref _linkedAssignments, value); }
    public ObservableCollection<Document> AttachedDocuments { get => _attachedDocuments; set => SetProperty(ref _attachedDocuments, value); }

    public ICommand SendCommand { get; }
    public ICommand NewSessionCommand { get; }
    public ICommand DeleteSessionCommand { get; }
    public ICommand ToggleSessionListCommand { get; }
    public ICommand ClearAttachmentCommand { get; }
    public ICommand ClearAssignmentCommand { get; }
    public ICommand RemoveLinkedAssignmentCommand { get; }
    public ICommand RemoveAttachedDocumentCommand { get; }

    public ChatViewModel(DatabaseService db, GeminiService gemini, DocumentService docService)
    {
        _db = db;
        _gemini = gemini;
        _docService = docService;

        SendCommand = new RelayCommand(async _ => await SendMessage(), _ => !IsSending && !string.IsNullOrWhiteSpace(MessageText));
        NewSessionCommand = new RelayCommand(_ => CreateNewSession());
        DeleteSessionCommand = new RelayCommand(p => DeleteSession(p as ChatSession));
        ToggleSessionListCommand = new RelayCommand(_ => ShowSessionList = !ShowSessionList);
        ClearAttachmentCommand = new RelayCommand(_ => AttachedDocument = null);
        ClearAssignmentCommand = new RelayCommand(_ => SelectedAssignment = null);
        RemoveLinkedAssignmentCommand = new RelayCommand(p => { if (p is Assignment a) LinkedAssignments.Remove(a); });
        RemoveAttachedDocumentCommand = new RelayCommand(p => { if (p is Document d) AttachedDocuments.Remove(d); });

        LoadSessions();
        LoadAvailableAssignments();
    }

    public void LoadSessions()
    {
        Sessions.Clear();
        foreach (var s in _db.GetChatSessions())
            Sessions.Add(s);
    }

    public void LoadAvailableAssignments()
    {
        AvailableAssignments.Clear();
        foreach (var a in _db.GetAssignments())
            AvailableAssignments.Add(a);
    }

    private void LoadMessages()
    {
        Messages.Clear();
        if (_currentSession == null) return;
        foreach (var m in _db.GetMessages(_currentSession.Id))
            Messages.Add(m);
    }

    private void CreateNewSession()
    {
        var id = _db.CreateChatSession("New Chat");
        LoadSessions();
        CurrentSession = Sessions.FirstOrDefault(s => s.Id == id);
    }

    private void DeleteSession(ChatSession? session)
    {
        if (session == null) return;
        _db.DeleteChatSession(session.Id);
        if (CurrentSession?.Id == session.Id)
        {
            CurrentSession = null;
            Messages.Clear();
        }
        LoadSessions();
    }

    public void LinkAssignment(Assignment assignment)
    {
        if (!LinkedAssignments.Any(a => a.Id == assignment.Id))
            LinkedAssignments.Add(assignment);
    }

    public void AttachDocument(Document document)
    {
        if (!AttachedDocuments.Any(d => d.Id == document.Id))
            AttachedDocuments.Add(document);
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText)) return;

        // Create session if none selected
        if (CurrentSession == null)
            CreateNewSession();

        IsSending = true;
        var userText = MessageText;
        MessageText = string.Empty;

        // Build context prompt
        var prompt = BuildPrompt(userText);

        // Save user message
        var userMsg = new Message
        {
            SessionId = CurrentSession!.Id,
            Role = "user",
            Content = userText
        };
        _db.AddMessage(userMsg);
        Messages.Add(userMsg);

        // Build history for API
        var history = Messages
            .Where(m => m != userMsg)
            .Select(m => (m.Role, m.Content))
            .ToList();

        // Create placeholder AI message for streaming
        var aiMsg = new Message
        {
            SessionId = CurrentSession.Id,
            Role = "assistant",
            Content = ""
        };
        Messages.Add(aiMsg);

        // Stream AI response
        var fullResponse = new System.Text.StringBuilder();
        await _gemini.SendMessageStreamAsync(prompt, history,
            chunk =>
            {
                fullResponse.Append(chunk);
                // Update message content on UI thread
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    aiMsg.Content = fullResponse.ToString();
                    // Trigger property changed on the message
                    var idx = Messages.IndexOf(aiMsg);
                    if (idx >= 0)
                    {
                        Messages.RemoveAt(idx);
                        Messages.Insert(idx, aiMsg);
                    }
                });
            },
            () =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    aiMsg.Content = fullResponse.ToString();
                    _db.AddMessage(aiMsg);
                });
            });

        // Auto-generate title if this is the first exchange
        if (Messages.Count <= 2 && CurrentSession.Title == "New Chat")
        {
            var title = await _gemini.GenerateTitleAsync(userText);
            _db.UpdateChatSessionTitle(CurrentSession.Id, title);
            LoadSessions();
            // Re-select current session
            CurrentSession = Sessions.FirstOrDefault(s => s.Id == CurrentSession.Id);
        }

        // Clear attachments after sending
        SelectedAssignment = null;
        AttachedDocument = null;
        LinkedAssignments.Clear();
        AttachedDocuments.Clear();
        IsSending = false;
    }

    private string BuildPrompt(string userMessage)
    {
        var sb = new System.Text.StringBuilder();

        // User profile
        var profile = _db.GetProfile();
        if (profile != null)
        {
            sb.AppendLine("## User Profile");
            sb.AppendLine($"Name: {profile.Name}");
            sb.AppendLine($"School: {profile.School}");
            sb.AppendLine();
        }

        // Referenced assignments (legacy single + new multi)
        if (SelectedAssignment != null)
        {
            sb.AppendLine("## Referenced Assignment");
            sb.AppendLine($"Title: {SelectedAssignment.Title}");
            sb.AppendLine($"Subject: {SelectedAssignment.Subject}");
            sb.AppendLine($"Class: {SelectedAssignment.ClassName}");
            sb.AppendLine($"Description: {SelectedAssignment.Description}");
            sb.AppendLine($"Brief: {SelectedAssignment.Brief}");
            sb.AppendLine($"Deadline: {SelectedAssignment.Deadline}");
            sb.AppendLine();
        }

        foreach (var assignment in LinkedAssignments)
        {
            sb.AppendLine($"## Linked Assignment: {assignment.Title}");
            sb.AppendLine($"Subject: {assignment.Subject}");
            sb.AppendLine($"Class: {assignment.ClassName}");
            sb.AppendLine($"Description: {assignment.Description}");
            sb.AppendLine($"Brief: {assignment.Brief}");
            sb.AppendLine($"Deadline: {assignment.Deadline}");
            sb.AppendLine();
        }

        // Attached documents
        if (AttachedDocument != null)
        {
            sb.AppendLine("## Attached Document Content");
            var text = _docService.ExtractText(AttachedDocument.FilePath);
            sb.AppendLine(text);
            sb.AppendLine();
        }

        foreach (var doc in AttachedDocuments)
        {
            sb.AppendLine($"## Attached Document: {doc.Filename}");
            var text = _docService.ExtractText(doc.FilePath);
            sb.AppendLine(text);
            sb.AppendLine();
        }

        sb.AppendLine("## User Message");
        sb.AppendLine(userMessage);

        return sb.ToString();
    }
}
