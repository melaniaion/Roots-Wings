using System.Text;
using System.Text.Json;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    public class GeminiSummaryService : IAiSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiSummaryService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"] ?? "";
        }

        public async Task<SummaryResult> SummariseEntriesAsync(
            List<JournalEntry> entries, TherapySession session)
        {
            // 1. Format the journal entries into readable text for the AI
            var entriesText = new StringBuilder();
            foreach (var entry in entries)
            {
                entriesText.AppendLine($"--- Entry: {entry.CreatedAt:MMMM d, yyyy} ---");
                entriesText.AppendLine($"Title: {entry.Title}");
                entriesText.AppendLine(entry.Content);
                entriesText.AppendLine();
            }

            // 2. The prompt
            var prompt = $"""
                You are an objective clinical assistant preparing a structured summary of a client's journal entries for their upcoming therapy session titled "{session.Title}" on {session.SessionDate:MMMM d, yyyy}.

                Analyze the following entries and extract only the most clinically relevant information. Avoid conversational filler or introductory/concluding text. Use a formal, professional tone.

                Client Journal Entries:
                {entriesText}

                Provide the summary using exactly these four section labels. Keep text concise and direct. Do not add text before or after the sections.

                ONE SENTENCE SUMMARY:
                [One precise, specific sentence capturing the primary emotional essence and core conflict of this period.]

                MAIN THEMES:
                [A brief paragraph of 2-3 sentences identifying recurring topics or situations, referencing specific details from the entries. Do not use bullet points here.]

                EMOTIONAL PATTERNS:
                [A brief paragraph of 2-3 sentences analyzing the emotional landscape, shifts, and triggers. Do not use bullet points here.]

                THINGS TO DISCUSS IN THERAPY:
                [Provide exactly 3-4 direct bullet points outlining specific, actionable items the client should bring up in their session based on the entries. Do not write an introductory paragraph here.]
                """;

            // 3. Gemini's request format
            // wrap the prompt in a contents/parts structure
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

            // 4. The API key goes in the URL for Gemini
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                      $"gemini-2.5-flash:generateContent?key={_apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            // 5. Send the request and read the response
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini error {response.StatusCode}: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);

            // 6. Gemini's response nests the text here:
            // candidates[0] -> content -> parts[0] -> text
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return ParseSummary(text);
        }

        // Finds each labelled section in the AI response and extracts the text
        private SummaryResult ParseSummary(string text)
        {
            return new SummaryResult
            {
                OneSentenceSummary = ExtractSection(text,
                    "ONE SENTENCE SUMMARY:", "MAIN THEMES:"),

                MainThemes = ExtractSection(text,
                    "MAIN THEMES:", "EMOTIONAL PATTERNS:"),

                EmotionalPatterns = ExtractSection(text,
                    "EMOTIONAL PATTERNS:", "THINGS TO DISCUSS IN THERAPY:"),

                ThingsToDiscuss = ExtractSection(text,
                    "THINGS TO DISCUSS IN THERAPY:", null)
            };
        }

        private string ExtractSection(string text, string startLabel, string? endLabel)
        {
            var start = text.IndexOf(startLabel, StringComparison.OrdinalIgnoreCase);
            if (start == -1) return "";

            start += startLabel.Length;

            var end = endLabel == null
                ? text.Length
                : text.IndexOf(endLabel, start, StringComparison.OrdinalIgnoreCase);

            if (end == -1) end = text.Length;

            return text[start..end].Trim();
        }
    }
}