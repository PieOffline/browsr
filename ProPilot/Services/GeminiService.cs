using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ProPilot.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient = new();
    private string _apiKey = string.Empty;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent";

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    /// <summary>
    /// Test the API key by sending a simple request.
    /// </summary>
    public async Task<(bool Success, string Message)> TestApiKeyAsync(string apiKey)
    {
        try
        {
            var url = $"{BaseUrl}?key={apiKey}";
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = "Say hello in one word." }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
                return (true, "API key is valid!");

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, $"API returned {response.StatusCode}: {errorBody}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a text message and get a response.
    /// </summary>
    public async Task<string> SendMessageAsync(string prompt, List<(string role, string content)>? history = null)
    {
        try
        {
            var url = $"{BaseUrl}?key={_apiKey}";
            var contents = new List<object>();

            if (history != null)
            {
                foreach (var (role, content) in history)
                {
                    contents.Add(new
                    {
                        role = role == "assistant" ? "model" : "user",
                        parts = new object[] { new { text = content } }
                    });
                }
            }

            contents.Add(new
            {
                role = "user",
                parts = new object[] { new { text = prompt } }
            });

            var payload = new { contents };
            var json = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"⚠️ API Error ({response.StatusCode}): {responseBody}";

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "No response received.";
        }
        catch (Exception ex)
        {
            return $"⚠️ Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Send an image with a text prompt (for assignment screenshot analysis).
    /// </summary>
    public async Task<string> SendImageAsync(string prompt, byte[] imageData, string mimeType = "image/png")
    {
        try
        {
            var url = $"{BaseUrl}?key={_apiKey}";
            var base64 = Convert.ToBase64String(imageData);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"⚠️ API Error ({response.StatusCode}): {responseBody}";

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "No response received.";
        }
        catch (Exception ex)
        {
            return $"⚠️ Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Generate a short title for a chat session from the first message.
    /// </summary>
    public async Task<string> GenerateTitleAsync(string firstMessage)
    {
        var prompt = $"Generate a very short (3-5 word) title for a chat that starts with this message. Return ONLY the title, no quotes or extra text:\n\n{firstMessage}";
        var result = await SendMessageAsync(prompt);
        // Clean up: remove quotes, trim
        return result.Trim().Trim('"').Trim('\'');
    }
}
