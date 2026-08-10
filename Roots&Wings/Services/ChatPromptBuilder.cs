using System.Text;
using System.Text.Json;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    public static class ChatPromptBuilder
    {
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
        // previous session post-notes first, then goals
        // If nothing is available it uses a general open.
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
        // character and its boundaries.
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
                    // Ask the model to close and list the conclusions.
                    // Uses a simple line format
                    messages.Add(new
                    {
                        role = "user",
                        content =
                            "That is enough for now. Write a short warm closing sentence, then list the talking points I could bring to my session.\n\n" +
                            "Rules for the talking points:\n" +
                            "- Give exactly 4.\n" +
                            "- Each one a full sentence of about 20 to 35 words, written in my voice starting with \"I\".\n" +
                            "- Each must draw on something DIFFERENT I said. Never repeat an idea.\n" +
                            "- Make each one specific and rich enough to genuinely open a conversation with my therapist.\n" +
                            "- Do not invent anything I did not say.\n\n" +
                            "Use exactly this format, with each point on its own line starting with a dash:\n" +
                            "CLOSING: <one warm sentence>\n" +
                            "POINTS:\n" +
                            "- I ...\n" +
                            "- I ...\n" +
                            "- I ...\n" +
                            "- I ..."
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

        // Parses the closing reply. Uses a simple line format with
        // several fallbacks, because getting nothing back would waste the
        // whole conversation for the user.
        public static ChatReply ParseClosing(string raw, string generatedBy)
        {
            var reply = new ChatReply { IsFinal = true, GeneratedBy = generatedBy };

            if (string.IsNullOrWhiteSpace(raw))
            {
                reply.Message = "Thank you for reflecting. Take what feels useful into your notes.";
                return reply;
            }

            var lines = raw.Replace("\r\n", "\n").Split('\n'); //Windows ends lines with \r\n, Mac/Linux with \n. This normalises everything to just \n
            var closingParts = new List<string>();   // lines that belong to the closing sentence
            var points = new List<string>();         // lines that are bullet points
            bool inPoints = false;                    // a flag: "have we reached the points yet?"

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;

                // Once we hit the POINTS label, everything after is a point.
                if (trimmed.StartsWith("POINTS", StringComparison.OrdinalIgnoreCase))
                {
                    inPoints = true;
                    continue;
                }

                // A dashed or bulleted line is a point wherever it appears.
                if (trimmed.StartsWith("-") || trimmed.StartsWith("•") || trimmed.StartsWith("*"))
                {
                    var cleaned = trimmed.TrimStart('-', '•', '*', ' ').Trim();
                    if (cleaned.Length > 0) points.Add(cleaned);
                    continue;
                }

                // A numbered line ("1. I ...") is also a point.
                if (trimmed.Length > 2 && char.IsDigit(trimmed[0]) &&
                    (trimmed[1] == '.' || trimmed[1] == ')'))
                {
                    var cleaned = trimmed.Substring(2).Trim();
                    if (cleaned.Length > 0) points.Add(cleaned);
                    continue;
                }

                if (!inPoints)
                {
                    // Part of the closing sentence. Strip the label if present.
                    var text = trimmed;
                    if (text.StartsWith("CLOSING:", StringComparison.OrdinalIgnoreCase))
                    {
                        text = text.Substring("CLOSING:".Length).Trim();
                    }
                    if (text.Length > 0) closingParts.Add(text);
                }
                else
                {
                    // Inside the points block but without a bullet marker.
                    points.Add(trimmed);
                }
            }

            reply.Message = closingParts.Count > 0
                ? string.Join(" ", closingParts)
                : "Thank you for reflecting. Take what feels useful into your notes.";

            // Remove near duplicates, comparing on a normalised form so the
            // same idea phrased twice does not appear twice.
            var seen = new HashSet<string>();
            foreach (var point in points)
            {
                var key = new string(point.ToLowerInvariant()
                    .Where(char.IsLetterOrDigit)
                    .ToArray());

                // Ignore very short fragments, they are rarely useful.
                if (key.Length > 15 && seen.Add(key))
                {
                    reply.Takeaways.Add(point);
                }
            }

            // Keep at most 5
            if (reply.Takeaways.Count > 5)
            {
                reply.Takeaways = reply.Takeaways.Take(5).ToList();
            }

            return reply;
        }
    }
}