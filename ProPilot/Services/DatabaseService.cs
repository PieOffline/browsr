using System.IO;
using Microsoft.Data.Sqlite;
using ProPilot.Models;

namespace ProPilot.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProPilot");
        Directory.CreateDirectory(appData);
        var dbPath = Path.Combine(appData, "propilot.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS profile (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                school TEXT NOT NULL,
                gemini_api_key TEXT NOT NULL,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS chat_sessions (
                id INTEGER PRIMARY KEY,
                title TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY,
                session_id INTEGER NOT NULL,
                role TEXT NOT NULL,
                content TEXT NOT NULL,
                created_at TEXT NOT NULL,
                FOREIGN KEY (session_id) REFERENCES chat_sessions(id)
            );

            CREATE TABLE IF NOT EXISTS assignments (
                id INTEGER PRIMARY KEY,
                title TEXT NOT NULL,
                subject TEXT NOT NULL,
                class_name TEXT NOT NULL,
                description TEXT NOT NULL,
                brief TEXT NOT NULL,
                deadline TEXT NOT NULL,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS documents (
                id INTEGER PRIMARY KEY,
                assignment_id INTEGER,
                filename TEXT NOT NULL,
                file_path TEXT NOT NULL,
                file_type TEXT NOT NULL,
                created_at TEXT NOT NULL,
                FOREIGN KEY (assignment_id) REFERENCES assignments(id)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    // ── Profile ──────────────────────────────────────────────

    public Profile? GetProfile()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, school, gemini_api_key, created_at FROM profile LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Profile
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                School = reader.GetString(3),
                GeminiApiKey = reader.GetString(4),
                CreatedAt = reader.GetString(5)
            };
        }
        return null;
    }

    public void SaveProfile(Profile profile)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO profile (id, name, email, school, gemini_api_key, created_at)
            VALUES (@id, @name, @email, @school, @key, @created)";
        cmd.Parameters.AddWithValue("@id", profile.Id == 0 ? 1 : profile.Id);
        cmd.Parameters.AddWithValue("@name", profile.Name);
        cmd.Parameters.AddWithValue("@email", profile.Email);
        cmd.Parameters.AddWithValue("@school", profile.School);
        cmd.Parameters.AddWithValue("@key", profile.GeminiApiKey);
        cmd.Parameters.AddWithValue("@created", profile.CreatedAt.Length > 0 ? profile.CreatedAt : DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    // ── Chat Sessions ────────────────────────────────────────

    public List<ChatSession> GetChatSessions()
    {
        var sessions = new List<ChatSession>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, title, created_at, updated_at FROM chat_sessions ORDER BY updated_at DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(new ChatSession
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                CreatedAt = reader.GetString(2),
                UpdatedAt = reader.GetString(3)
            });
        }
        return sessions;
    }

    public long CreateChatSession(string title)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var now = DateTime.UtcNow.ToString("o");
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO chat_sessions (title, created_at, updated_at)
            VALUES (@title, @now, @now);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@now", now);
        return (long)cmd.ExecuteScalar()!;
    }

    public void UpdateChatSessionTitle(long id, string title)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE chat_sessions SET title = @title, updated_at = @now WHERE id = @id";
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteChatSession(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM messages WHERE session_id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();

        cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM chat_sessions WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ── Messages ─────────────────────────────────────────────

    public List<Message> GetMessages(long sessionId)
    {
        var messages = new List<Message>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, session_id, role, content, created_at FROM messages WHERE session_id = @sid ORDER BY created_at ASC";
        cmd.Parameters.AddWithValue("@sid", sessionId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            messages.Add(new Message
            {
                Id = reader.GetInt64(0),
                SessionId = reader.GetInt64(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAt = reader.GetString(4)
            });
        }
        return messages;
    }

    public void AddMessage(Message message)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO messages (session_id, role, content, created_at)
            VALUES (@sid, @role, @content, @created)";
        cmd.Parameters.AddWithValue("@sid", message.SessionId);
        cmd.Parameters.AddWithValue("@role", message.Role);
        cmd.Parameters.AddWithValue("@content", message.Content);
        cmd.Parameters.AddWithValue("@created", message.CreatedAt.Length > 0 ? message.CreatedAt : DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();

        // Touch session updated_at
        cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE chat_sessions SET updated_at = @now WHERE id = @sid";
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@sid", message.SessionId);
        cmd.ExecuteNonQuery();
    }

    // ── Assignments ──────────────────────────────────────────

    public List<Assignment> GetAssignments()
    {
        var list = new List<Assignment>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, title, subject, class_name, description, brief, deadline, created_at FROM assignments ORDER BY created_at DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Assignment
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Subject = reader.GetString(2),
                ClassName = reader.GetString(3),
                Description = reader.GetString(4),
                Brief = reader.GetString(5),
                Deadline = reader.GetString(6),
                CreatedAt = reader.GetString(7)
            });
        }
        return list;
    }

    public long CreateAssignment(Assignment a)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO assignments (title, subject, class_name, description, brief, deadline, created_at)
            VALUES (@title, @subject, @class, @desc, @brief, @deadline, @created);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@title", a.Title);
        cmd.Parameters.AddWithValue("@subject", a.Subject);
        cmd.Parameters.AddWithValue("@class", a.ClassName);
        cmd.Parameters.AddWithValue("@desc", a.Description);
        cmd.Parameters.AddWithValue("@brief", a.Brief);
        cmd.Parameters.AddWithValue("@deadline", a.Deadline);
        cmd.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o"));
        return (long)cmd.ExecuteScalar()!;
    }

    public void UpdateAssignment(Assignment a)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE assignments
            SET title=@title, subject=@subject, class_name=@class, description=@desc, brief=@brief, deadline=@deadline
            WHERE id=@id";
        cmd.Parameters.AddWithValue("@title", a.Title);
        cmd.Parameters.AddWithValue("@subject", a.Subject);
        cmd.Parameters.AddWithValue("@class", a.ClassName);
        cmd.Parameters.AddWithValue("@desc", a.Description);
        cmd.Parameters.AddWithValue("@brief", a.Brief);
        cmd.Parameters.AddWithValue("@deadline", a.Deadline);
        cmd.Parameters.AddWithValue("@id", a.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteAssignment(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        // Unlink documents
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE documents SET assignment_id = NULL WHERE assignment_id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();

        cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM assignments WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ── Documents ────────────────────────────────────────────

    public List<Document> GetDocuments()
    {
        var list = new List<Document>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, assignment_id, filename, file_path, file_type, created_at FROM documents ORDER BY created_at DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Document
            {
                Id = reader.GetInt64(0),
                AssignmentId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                Filename = reader.GetString(2),
                FilePath = reader.GetString(3),
                FileType = reader.GetString(4),
                CreatedAt = reader.GetString(5)
            });
        }
        return list;
    }

    public List<Document> GetDocumentsByAssignment(long assignmentId)
    {
        var list = new List<Document>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, assignment_id, filename, file_path, file_type, created_at FROM documents WHERE assignment_id = @aid ORDER BY created_at DESC";
        cmd.Parameters.AddWithValue("@aid", assignmentId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Document
            {
                Id = reader.GetInt64(0),
                AssignmentId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                Filename = reader.GetString(2),
                FilePath = reader.GetString(3),
                FileType = reader.GetString(4),
                CreatedAt = reader.GetString(5)
            });
        }
        return list;
    }

    public long AddDocument(Document doc)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO documents (assignment_id, filename, file_path, file_type, created_at)
            VALUES (@aid, @name, @path, @type, @created);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@aid", doc.AssignmentId.HasValue ? (object)doc.AssignmentId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@name", doc.Filename);
        cmd.Parameters.AddWithValue("@path", doc.FilePath);
        cmd.Parameters.AddWithValue("@type", doc.FileType);
        cmd.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o"));
        return (long)cmd.ExecuteScalar()!;
    }

    public void LinkDocumentToAssignment(long docId, long? assignmentId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE documents SET assignment_id = @aid WHERE id = @id";
        cmd.Parameters.AddWithValue("@aid", assignmentId.HasValue ? (object)assignmentId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@id", docId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteDocument(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM documents WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ── Reset ────────────────────────────────────────────────

    public void ResetAllData()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM messages;
            DELETE FROM chat_sessions;
            DELETE FROM documents;
            DELETE FROM assignments;
            DELETE FROM profile;
        ";
        cmd.ExecuteNonQuery();
    }
}
