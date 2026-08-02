using System.Text;
using System.Text.Json;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // Builds the prompts for the reflection dialogue. This is a guided
    // Socratic exchange: the assistant asks open questions to help the user
    // think, rather than giving advice. It never diagnoses or instructs.
    //
    // The dialogue is deliberately short. After a small number of exchanges
    // it closes and offers the user a few takeaways drawn from their own words.
    public static class ChatPromptBuilder
    {
        // Hard cap on how many questions the assistant asks before closing.
        // Kept small so the user does not get tired, and so a small model
        // stays coherent across the whole exchange.
        public const int MaxAssistantQuestions = 4;

        // Shapes the STYLE of questioning to match the user's therapy type.
        // The technique (Socratic questioning) is the same; what it leans
        // toward differs by modality, mirroring how each approach works.
        private static string LensFor(string? therapyType)
        {
            switch (therapyType)
            {
                case "CBT":
                    return "Lean gently toward specific thoughts and the situations that triggered them, "
                         + "in the spirit of Socratic questioning used in CBT.";
                case "DBT":
                    return "Lean gently toward moments of strong emotion and what helped them cope.";
                case "ACT":
                    return "Lean gently toward what matters to them and what they may be avoiding.";
                case "IFS":
                    return "Lean gently toward the different sides of themselves that show up, in plain language.";
                default:
                    return "Simply follow what feels most present for them.";
            }
        }

        // Assembles the context the assistant opens from. Uses a priority
        // ladder: previous session takeaways first, then goals, always the
        // therapy lens. If nothing is available it degrades to a general open.
        private static string BuildContext(
            TherapySession session,
            TherapySession? previousSession,
            List<Goal> openGoals)
        {
            var sb = new StringBuilder();

            if (previousSession != null && !string.IsNullOrWhiteSpace(previousSession.PostNotes))
            {
                sb.AppendLine($"After their last session the user wrote: \"{previousSession.PostNotes}\"");
            }

            if (openGoals.Any())
            {
                var goalTitles = string.Join("; ", openGoals.Select(g => g.Title));
                sb.AppendLine($"They are currently working on these goals: {goalTitles}.");
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("There is no earlier history to draw on yet; this may be an early session.");
            }

            return sb.ToString();
        }

        // The system instruction sent on every turn. Defines the assistant's
        // character and its hard boundaries.
        private static string SystemInstruction(TherapySession session)
        {
            return $"""
                You are a warm, gentle reflection companion inside a therapy preparation app. You are NOT a therapist. You help the user think about what they might want to bring to their upcoming session by asking open, Socratic questions, one at a time.

                Firm rules:
                - Ask ONE short, open question per reply. Never more than one.
                - Never give advice, opinions, interpretations, reassurance, or diagnoses. Only ask, and briefly acknowledge what they said before asking.
                - Keep each reply to two or three sentences at most.
                - Stay with what the user actually says. Do not invent details about their life.
                - Warm, plain language. No clinical terms. Speak to them as "you".

                {LensFor(session.TherapyType)}
                """;
        }

        // Builds the full message list for the AI, given the conversation so
        // far. When the transcript is empty, the assistant produces its
        // opening question from the context. On later turns it continues.
        public static object BuildRequestMessages(
            TherapySession session,
            TherapySession? previousSession,
            List<Goal> openGoals,
            List<ChatTurn> transcript)
        {
            var messages = new List<object>();

            messages.Add(new { role = "system", content = SystemInstruction(session) });

            // How many questions has the assistant already asked?
            int askedSoFar = transcript.Count(t => t.Role == "assistant");
            bool shouldClose = askedSoFar >= MaxAssistantQuestions;

            if (transcript.Count == 0)
            {
                // Opening turn: give the model the context and ask it to open.
                var context = BuildContext(session, previousSession, openGoals);
                messages.Add(new
                {
                    role = "user",
                    content =
                        "Here is what you know about me:\n" + context +
                        "\nGently open the reflection with a single warm question to help me start preparing for my session."
                });
            }
            else
            {
                // Replay the conversation so far.
                foreach (var turn in transcript)
                {
                    messages.Add(new { role = turn.Role, content = turn.Content });
                }

                if (shouldClose)
                {
                    // Ask the model to close and produce takeaways as JSON so
                    // we can render them as pick-able chips.
                    messages.Add(new
                    {
                        role = "user",
                        content =
                            "That is enough for now. Reply with a short warm closing sentence, then on a new line output ONLY a JSON array " +
                            "of 3 to 4 short first-person phrases capturing things I might want to bring to my session, drawn strictly from what I said above. " +
                            "Format exactly: CLOSING: <one sentence>\nTAKEAWAYS: [\"...\",\"...\"]"
                    });
                }
            }

            return new { messages, shouldClose };
        }

        // Parses a normal (non-closing) assistant reply.
        public static ChatReply ParseQuestion(string raw, string generatedBy)
        {
            return new ChatReply
            {
                Message = raw.Trim(),
                IsFinal = false,
                GeneratedBy = generatedBy
            };
        }

        // Parses the closing reply, splitting the closing line from the
        // JSON takeaways. Falls back gracefully if the model does not format
        // the JSON perfectly.
        public static ChatReply ParseClosing(string raw, string generatedBy)
        {
            var reply = new ChatReply { IsFinal = true, GeneratedBy = generatedBy };

            var closingMarker = "CLOSING:";
            var takeawayMarker = "TAKEAWAYS:";

            var takeIdx = raw.IndexOf(takeawayMarker, StringComparison.OrdinalIgnoreCase);

            if (takeIdx == -1)
            {
                // Model ignored the format. Use the whole thing as the closing
                // message and offer no chips rather than crashing.
                reply.Message = raw.Replace(closingMarker, "").Trim();
                return reply;
            }

            var closingPart = raw.Substring(0, takeIdx);
            var closingStart = closingPart.IndexOf(closingMarker, StringComparison.OrdinalIgnoreCase);
            reply.Message = closingStart == -1
                ? closingPart.Trim()
                : closingPart.Substring(closingStart + closingMarker.Length).Trim();

            var jsonPart = raw.Substring(takeIdx + takeawayMarker.Length).Trim();

            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(jsonPart);
                if (parsed != null)
                {
                    reply.Takeaways = parsed
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToList();
                }
            }
            catch
            {
                // Bad JSON: leave takeaways empty, keep the closing message.
                // The user still gets a graceful end to the dialogue.
            }

            return reply;
        }
    }
}
