using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // Primary provider for the reflection dialogue. Uses Groq (Llama 3.1 8B),
    // OpenAI shaped chat completions. The prompt building and parsing live in
    // ChatPromptBuilder so this class only handles talking to the API.
    public class GroqChatService : IAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public const string ProviderName = "Groq (Llama 3.1 8B)";

        public GroqChatService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Groq:ApiKey"] ?? "";
        }

        public async Task<ChatReply> ContinueDialogueAsync(
            TherapySession session,
            TherapySession? previousSession,
            List<Goal> openGoals,
            List<ChatTurn> transcript)
        {
            // The builder returns the message list plus whether this is the
            // closing turn. We read both out of the anonymous object via JSON
            // round trip to keep the builder framework free.
            var built = ChatPromptBuilder.BuildRequestMessages(
                session, previousSession, openGoals, transcript);

            var builtJson = JsonSerializer.Serialize(built);
            using var builtDoc = JsonDocument.Parse(builtJson);
            var messagesElement = builtDoc.RootElement.GetProperty("messages");
            bool shouldClose = builtDoc.RootElement.GetProperty("shouldClose").GetBoolean();

            var requestBody = new
            {
                model = "llama-3.1-8b-instant",
                messages = messagesElement,
                temperature = 0.6,
                max_tokens = 500
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Groq chat error {response.StatusCode}: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("Groq chat returned an empty message.");
            }

            return shouldClose
                ? ChatPromptBuilder.ParseClosing(text, ProviderName)
                : ChatPromptBuilder.ParseQuestion(text, ProviderName);
        }
    }
}
