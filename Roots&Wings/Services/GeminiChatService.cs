using System.Text;
using System.Text.Json;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // Fallback provider for the dialogue. Gemini uses a different
    // request shape (contents/parts with roles) than the OpenAI style, so the
    // messages built by ChatPromptBuilder are translated here. The system
    // instruction is folded into the first user turn because Gemini handles
    // system prompts differently.
    public class GeminiChatService : IAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public const string ProviderName = "Gemini";

        public GeminiChatService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"] ?? "";
        }

        public async Task<ChatReply> ContinueDialogueAsync(
            TherapySession session,
            TherapySession? previousSession,
            List<Goal> openGoals,
            List<ChatTurn> transcript)
        {
            var built = ChatPromptBuilder.BuildRequestMessages(
                session, previousSession, openGoals, transcript);

            var builtJson = JsonSerializer.Serialize(built);
            using var builtDoc = JsonDocument.Parse(builtJson);
            var messagesElement = builtDoc.RootElement.GetProperty("messages");
            bool shouldClose = builtDoc.RootElement.GetProperty("shouldClose").GetBoolean();

            // Translate the OpenAI style messages into Gemini's contents.
            // Gemini roles are "user" and "model" (not "assistant"), and there
            // is no separate system role, so a system message becomes a user
            // message at the front.
            var contents = new List<object>();
            foreach (var msg in messagesElement.EnumerateArray())
            {
                var role = msg.GetProperty("role").GetString() ?? "user";
                var content = msg.GetProperty("content").GetString() ?? "";

                var geminiRole = role switch
                {
                    "assistant" => "model",
                    "system" => "user",
                    _ => "user"
                };

                contents.Add(new
                {
                    role = geminiRole,
                    parts = new[] { new { text = content } }
                });
            }

            var requestBody = new { contents };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                      $"gemini-2.5-flash:generateContent?key={_apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini chat error {response.StatusCode}: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return shouldClose
                ? ChatPromptBuilder.ParseClosing(text, ProviderName)
                : ChatPromptBuilder.ParseQuestion(text, ProviderName);
        }
    }
}
