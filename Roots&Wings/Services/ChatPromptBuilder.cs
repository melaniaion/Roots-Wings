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
        // Long enough for the conversation to actually develop, short enough
        // that the user does not get tired and a small model stays coherent.
        public const int MaxAssistantQuestions = 6;

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
                You are a warm, perceptive reflection companion inside a therapy preparation app. You are NOT a therapist. Your job is to help someone work out what they want to bring to their upcoming session, by asking one thoughtful open question at a time.

                HOW TO ASK WELL:
                - Ask exactly ONE question per reply. Never two.
                - Begin with a short, warm lead-in of one sentence that eases into the question, then ask it.
                - Go DEEPER into what they just said. Pick up the most specific or most charged detail in their answer and ask about that particular thing, not the general topic.
                - Prefer questions that open something new: what sits underneath a feeling, when it started, what it would mean if it were true, what they would want instead, what makes this one harder than others.

                THE LEAD-IN, DONE RIGHT:
                The opening sentence must ADD something, not replay what they said. Good lead-ins notice a thread running through their answers, name a tension between two things they said, connect back to something from earlier in the conversation, or gently frame why you are asking next. For example: "There seems to be something about how others see you running underneath this." or "That sounds like it pulls in two directions at once."

                NEVER DO THIS (it feels robotic and unhelpful):
                - Do NOT restate, paraphrase, mirror, or summarise what they just told you. If they say they feel awkward, do not reply "You feel awkward about this." Just move on and ask.
                - Do NOT begin with "It sounds like you...", "You mentioned...", "You're feeling...", "So you...", or any variation that simply plays their own words back at them.
                - Do NOT praise, reassure, validate, advise, interpret, or diagnose.
                - Do NOT ask about something they already answered.

                If they say they are unsure or do not know, do not push the same question again. Move to a different angle, or make the question smaller and more concrete.

                STYLE:
                - Two or three sentences: a short lead-in, then the question.
                - Warm, plain, human language. No clinical terms. Address them as "you".

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
                            "That is enough for now. Write a short warm closing sentence, then a JSON array of talking points I could bring to my session.\n\n" +
                            "Rules for the talking points:\n" +
                            "- Give 5 or 6 of them.\n" +
                            "- Each one a full, specific sentence of about 12 to 25 words, written in my voice starting with \"I\".\n" +
                            "- Draw each from something different I actually said. Do not repeat the same idea in two points.\n" +
                            "- Make them concrete enough to actually open a conversation with my therapist, not vague labels.\n" +
                            "- Do not invent anything I did not say.\n\n" +
                            "Format exactly like this and nothing else:\n" +
                            "CLOSING: <one sentence>\n" +
                            "TAKEAWAYS: [\"I ...\",\"I ...\",\"I ...\",\"I ...\",\"I ...\"]"
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
                    // Clean up and remove near duplicates. The model sometimes
                    // repeats the same idea, so we compare on a normalised
                    // form (lowercase, no punctuation) and keep the first.
                    var seen = new HashSet<string>();
                    foreach (var raw2 in parsed)
                    {
                        if (string.IsNullOrWhiteSpace(raw2)) continue;

                        var cleaned = raw2.Trim();
                        var key = new string(cleaned.ToLowerInvariant()
                            .Where(char.IsLetterOrDigit)
                            .ToArray());

                        if (key.Length > 0 && seen.Add(key))
                        {
                            reply.Takeaways.Add(cleaned);
                        }
                    }
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