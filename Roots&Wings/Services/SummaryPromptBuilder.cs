using System.Text;
using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    public static class SummaryPromptBuilder
    {
        // The section labels. The prompt asks for these exact strings and the
        // parser looks for these exact strings. They must always match.
        public const string LabelOverall = "OVERALL:";
        public const string LabelThemes = "WHAT KEPT COMING UP:";
        public const string LabelDiscuss = "MIGHT BRING UP:";

        // Tells the model what to pay attention to when writing the last
        // section considering the therapy type.
        private static string LensFor(string? therapyType)
        {
            switch (therapyType)
            {
                case "CBT":
                    return "The user is in Cognitive Behavioural Therapy. When suggesting things to bring up, "
                         + "pay attention to specific thoughts and the situations that triggered them and to "
                         + "recurring patterns of thinking that might be worth examining together.";

                case "DBT":
                    return "The user is in Dialectical Behaviour Therapy. When suggesting things to bring up, "
                         + "pay attention to moments of intense or overwhelming emotion, what happened around "
                         + "them and anything the user did that seemed to help them cope.";

                case "ACT":
                    return "The user is in Acceptance and Commitment Therapy. When suggesting things to bring up, "
                         + "pay attention to what seems to matter to the user, what they move toward and what "
                         + "they seem to be avoiding or pushing away.";

                case "IFS":
                    return "The user is in Internal Family Systems therapy. When suggesting things to bring up, "
                         + "pay attention to different sides of the user that show up in the writing, such as a "
                         + "critical voice or a part that wants to withdraw, described in plain everyday language.";

                default:
                    // Covers Unsure and anything unrecognised.
                    return "The user has not specified a particular type of therapy. When suggesting things to "
                         + "bring up, simply follow what appears most present and most alive in the writing.";
            }
        }

        public static string Build(List<JournalEntry> entries, TherapySession session)
        {
            // Format the entries. Tags are attached to the entry they belong to
            // so the model can connect a feeling to the writing it came from,
            // rather than having to reason about a separate list.
            var entriesText = new StringBuilder();
            foreach (var entry in entries)
            {
                entriesText.AppendLine($"--- Entry: {entry.CreatedAt:MMMM d, yyyy} ---");
                entriesText.AppendLine($"Title: {entry.Title}");

                if (entry.Tags != null && entry.Tags.Any())
                {
                    var tagNames = string.Join(", ", entry.Tags.Select(t => t.Name));
                    entriesText.AppendLine($"How they tagged it: {tagNames}");
                }

                entriesText.AppendLine(entry.Content);
                entriesText.AppendLine();
            }

            // Counting is done here rather than by the model. Small models are
            // unreliable at counting across a list.
            var tagCounts = entries
                .Where(e => e.Tags != null)
                .SelectMany(e => e.Tags)
                .GroupBy(t => t.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            var tagSummaryLine = tagCounts.Any()
                ? $"Across these entries the user chose these feeling tags: {string.Join(", ", tagCounts)}."
                : "The user did not tag these entries with any feelings.";

            var lens = LensFor(session.TherapyType);


            var prompt = $"""
                You are helping someone get ready for their therapy session. You are not a therapist and you are not assessing them. Your job is to organise their own journal writing and reflect it back so it is easier for them to remember and decide what to talk about.

                Write in a warm but factual voice, speaking to the person directly as "you". Report what is there. Do not interpret, diagnose, assess, advise or reassure. Do not use clinical language.

                Use only what appears in the entries below. Never invent details, feelings or patterns. If there is very little material, write less rather than filling space. Do not describe something mentioned once as a recurring pattern.

                {tagSummaryLine}

                {lens}

                Their journal entries:
                {entriesText}

                Reply using exactly these three labels, in this order, with no text before, between or after them beyond the sections themselves.

                {LabelOverall}
                [One or two plain sentences describing the overall shape of this period. Orienting, not dramatic.]

                {LabelThemes}
                [Two or three short paragraphs covering what came up repeatedly across the entries, referring to specific things they wrote about. Where a feeling tag lines up with what they wrote, mention it naturally. Include anything that seemed to go well or help, not only what was difficult. Write in flowing sentences, not bullet points.]

                {LabelDiscuss}
                [Three or four short bullet points, each one thing they might choose to bring up. Phrase each as an invitation, for example "You might want to talk about..." or "It could be worth mentioning...". These are options for them to pick from, not instructions. Do not tell them what to do about anything.]
                """;

            return prompt;
        }

        // Slices the model's reply into the three sections.
        // Shared by both providers so parsing behaviour is identical.
        public static SummaryResult Parse(string text, string generatedBy)
        {
            return new SummaryResult
            {
                Overall = ExtractSection(text, LabelOverall, LabelThemes),
                WhatKeptComingUp = ExtractSection(text, LabelThemes, LabelDiscuss),
                MightBringUp = ExtractSection(text, LabelDiscuss, null),
                GeneratedBy = generatedBy
            };
        }

        private static string ExtractSection(string text, string startLabel, string? endLabel)
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