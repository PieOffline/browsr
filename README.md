# ProPilot

**Your smart study co-pilot 🚀** — a C# WPF desktop application for students to manage assignments, upload documents, and chat with an AI that understands their context.

## Features

- **AI Chat** — Chat with Google Gemini AI with full context injection (assignments, documents, user profile), streamed word-by-word responses
- **Assignment Manager** — Create assignments manually or import from screenshots using AI analysis
- **Document Manager** — Upload PDF and DOCX files, extract text, link to assignments
- **@ Mention Linking** — Type `@` in chat to link assignments, or use the icon button
- **Onboarding Wizard** — Guided setup for profile and API key configuration
- **Discord × Duolingo UI** — Dark themed interface with Indigo-Violet gradient accents, liquid glass pill navigation, and spring animations

## Tech Stack

- **Language:** C# (.NET 8)
- **UI Framework:** WPF + [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- **AI API:** Google Gemini API (`gemini-2.0-flash`) with SSE streaming
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

## Data Storage

All persistent data is stored **inside the project/repo folder** in a `data/` subfolder relative to the application's base directory:

```
<app-base-directory>/data/
├── propilot.db          # SQLite database (profile, chats, assignments, documents)
└── Documents/           # Uploaded PDF/DOCX files
```

This makes it easy to debug, inspect, and wipe data — just look in the `data/` folder. The `data/` folder is git-ignored by default.

### Project Structure

```
ProPilot/
├── Models/          # Data models (Profile, ChatSession, Message, Assignment, Document)
├── Services/        # Business logic (DatabaseService, GeminiService, DocumentService)
├── ViewModels/      # MVVM ViewModels for each view
├── Views/           # WPF UserControls (XAML + code-behind)
├── Helpers/         # MVVM base classes (RelayCommand, ViewModelBase)
├── Converters/      # WPF value converters
├── App.xaml         # Theme configuration (Dark, Indigo-Violet accent)
└── MainWindow.xaml  # Main window with top pill navigation
```

## Architecture

The app uses the **MVVM pattern** with:
- **ViewModelBase** for property change notification
- **RelayCommand** for command binding
- **DataTemplates** in MainWindow for automatic view/viewmodel mapping
- **SQLite** for persistent local storage
- **GUID-based session IDs** for reliable chat session management

## Navigation

The app uses a **top navigation bar** with liquid glass pill tabs:
1. **Chat** — AI chat interface with session management and streaming responses
2. **Assignments** — Assignment cards with screenshot import and AI analysis
3. **Documents** — PDF/DOCX upload and management
4. **Settings** — Profile, API key, data management, and about info

## Design

- **Theme:** Discord × Duolingo hybrid — dark, fun, rounded, vibrant
- **Colours:** Deep dark base (`#1A1A2E`), panel (`#16213E`), Indigo-Violet gradient accents (`#6C63FF` → `#A78BFA`)
- **Typography:** Roboto via MaterialDesignThemes
- **Animations:** Hover glow effects, smooth transitions
