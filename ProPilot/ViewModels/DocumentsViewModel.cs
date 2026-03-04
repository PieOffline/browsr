using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using ProPilot.Helpers;
using ProPilot.Models;
using ProPilot.Services;

namespace ProPilot.ViewModels;

public class DocumentsViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly DocumentService _docService;

    public ObservableCollection<Document> Documents { get; } = new();
    public ObservableCollection<Assignment> Assignments { get; } = new();

    public ICommand UploadCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand LinkCommand { get; }

    public DocumentsViewModel(DatabaseService db, DocumentService docService)
    {
        _db = db;
        _docService = docService;

        UploadCommand = new RelayCommand(_ => { }); // Will be handled in code-behind for file dialog
        DeleteCommand = new RelayCommand(p => DeleteDocument(p));
        OpenCommand = new RelayCommand(p => OpenDocument(p));
        LinkCommand = new RelayCommand(p => { }); // Handled via binding

        LoadDocuments();
    }

    public void LoadDocuments()
    {
        Documents.Clear();
        foreach (var d in _db.GetDocuments())
            Documents.Add(d);

        Assignments.Clear();
        foreach (var a in _db.GetAssignments())
            Assignments.Add(a);
    }

    public void UploadFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".docx") return;

        // Copy to app data folder
        var docsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "Documents");
        Directory.CreateDirectory(docsFolder);
        var destPath = Path.Combine(docsFolder, Path.GetFileName(filePath));

        // Handle duplicate names
        var counter = 1;
        while (File.Exists(destPath))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            destPath = Path.Combine(docsFolder, $"{nameWithoutExt}_{counter}{ext}");
            counter++;
        }

        File.Copy(filePath, destPath);

        var doc = new Document
        {
            Filename = Path.GetFileName(destPath),
            FilePath = destPath,
            FileType = ext.TrimStart('.')
        };

        _db.AddDocument(doc);
        LoadDocuments();
    }

    public void LinkDocumentToAssignment(long docId, long? assignmentId)
    {
        _db.LinkDocumentToAssignment(docId, assignmentId);
        LoadDocuments();
    }

    private void DeleteDocument(object? param)
    {
        if (param is Document doc)
        {
            _db.DeleteDocument(doc.Id);
            // Try to delete the actual file
            try { if (File.Exists(doc.FilePath)) File.Delete(doc.FilePath); } catch { }
            LoadDocuments();
        }
    }

    private void OpenDocument(object? param)
    {
        if (param is Document doc && File.Exists(doc.FilePath))
        {
            Process.Start(new ProcessStartInfo(doc.FilePath) { UseShellExecute = true });
        }
    }
}
