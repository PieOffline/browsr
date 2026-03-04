# ProPilot

**Your personal AI study assistant** — a C# WPF desktop application for students to manage assignments, upload documents, and chat with an AI that understands their context.

## Features

- **AI Chat** — Chat with Google Gemini AI with full context injection (assignments, documents, user profile)
- **Assignment Manager** — Create assignments manually or import from screenshots using AI analysis
- **Document Manager** — Upload PDF and DOCX files, extract text, link to assignments
- **Onboarding Wizard** — Guided setup for profile and API key configuration
- **Material Design UI** — Modern dark-themed interface with Teal/Cyan accent colours

## Tech Stack

- **Language:** C# (.NET 8)
- **UI Framework:** WPF + [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- **AI API:** Google Gemini API (`gemini-2.0-flash`)
- **PDF handling:** PdfPig
- **Word documents:** DocumentFormat.OpenXml
- **Database:** SQLite via Microsoft.Data.Sqlite
- **Markdown:** Markdig

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Windows 10/11 (WPF requires Windows)
- A [Google Gemini API key](https://aistudio.google.com/apikey) (free)

### Build & Run

```bash
dotnet restore ProPilot/ProPilot.csproj
dotnet build ProPilot/ProPilot.csproj
dotnet run --project ProPilot/ProPilot.csproj
```

### Project Structure

```
ProPilot/
├── Models/          # Data models (Profile, ChatSession, Message, Assignment, Document)
├── Services/        # Business logic (DatabaseService, GeminiService, DocumentService)
├── ViewModels/      # MVVM ViewModels for each view
├── Views/           # WPF UserControls (XAML + code-behind)
├── Helpers/         # MVVM base classes (RelayCommand, ViewModelBase)
├── Converters/      # WPF value converters
├── App.xaml         # Material Design theme configuration
└── MainWindow.xaml  # Main application window with sidebar navigation
```

## Architecture

The app uses the **MVVM pattern** with:
- **ViewModelBase** for property change notification
- **RelayCommand** for command binding
- **DataTemplates** in MainWindow for automatic view/viewmodel mapping
- **SQLite** for persistent local storage

## Navigation

The app has a left sidebar with icon buttons:
1. **Chat** — AI chat interface with session management
2. **Assignments** — Assignment cards with screenshot import
3. **Documents** — PDF/DOCX upload and management
4. **Settings** — Profile, API key, and data management
