using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // Primary AI provider. Groq uses the same request shape as the OpenAI API,
    // so the body is a list of messages, compared to Gemini's contents/parts.
    public class GroqSummaryService : IAiSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Shown to the user so they can see which provider produced the text.
        public const string ProviderName = "Groq (Llama 3.1 8B)";

        public GroqSummaryService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Groq:ApiKey"] ?? "";
        }

        public async Task<SummaryResult> SummariseEntriesAsync(
            List<JournalEntry> entries, TherapySession session)
        {
            var prompt = SummaryPromptBuilder.Build(entries, session);

            var requestBody = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                // Lower temperature keeps the output closer to what was written
                // and reduces the chance of the model inventing details.
                temperature = 0.4,
                max_tokens = 1200
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");

            // Unlike Gemini, the key goes in an Authorization header.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Groq error {response.StatusCode}: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);

            // OpenAI shaped response: choices[0] -> message -> content
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var result = SummaryPromptBuilder.Parse(text, ProviderName);

            // If the model ignored the format, the sections come back empty.
            // Treat that as a failure so the wrapper can fall back to the other
            // provider instead of showing the user a blank document.
            if (string.IsNullOrWhiteSpace(result.Overall) &&
                string.IsNullOrWhiteSpace(result.WhatKeptComingUp) &&
                string.IsNullOrWhiteSpace(result.MightBringUp))
            {
                throw new Exception("Groq returned a response that could not be parsed into sections.");
            }

            return result;
        }
    }
}