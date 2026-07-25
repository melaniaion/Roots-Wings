using System.Text;
using System.Text.Json;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // Fallback AI provider. Used only when the primary provider fails.
    // The prompt and the parsing now come from SummaryPromptBuilder, so both
    // providers ask for exactly the same thing and return the same shape.
    public class GeminiSummaryService : IAiSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public const string ProviderName = "Gemini";

        public GeminiSummaryService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"] ?? "";
        }

        public async Task<SummaryResult> SummariseEntriesAsync(
            List<JournalEntry> entries, TherapySession session)
        {
            var prompt = SummaryPromptBuilder.Build(entries, session);

            // Gemini wraps the prompt in a contents/parts structure
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            // The API key goes in the URL for Gemini
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                      $"gemini-2.5-flash:generateContent?key={_apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini error {response.StatusCode}: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);

            // Gemini's response nests the text here:
            // candidates[0] -> content -> parts[0] -> text
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return SummaryPromptBuilder.Parse(text, ProviderName);
        }
    }
}