using Roots_Wings.DTOs;
using Roots_Wings.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Roots_Wings.Services
{
    public class PdfExportService : IPdfExportService
    {
        // Generates the PDF bytes and returns them.
        // The controller will stream these directly to the browser as a download.
        public byte[] GenerateSummaryPdf(
            TherapySession session,
            SummaryResult summary,
            string[] includeBlocks)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.2f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x
                        .FontSize(10.5f)
                        .FontColor("#5C3D2E")
                        .LineHeight(1.7f));

                    page.Content().Column(col =>
                    {
                        // ── Letterhead ──
                        col.Item()
                            .PaddingBottom(6)
                            .Text("Prepared for therapy")
                            .FontSize(7.5f)
                            .LetterSpacing(0.12f)
                            .FontColor("#B5A898")
                            .Italic();

                        col.Item()
                            .Text(session.Title)
                            .FontSize(26)
                            .Italic()
                            .FontColor("#5C3D2E");

                        col.Item()
                            .PaddingTop(4)
                            .Text(txt =>
                            {
                                txt.Span(session.SessionDate.ToString("dddd, MMMM d, yyyy"))
                                    .FontSize(9.5f)
                                    .FontColor("#8B7B6A");

                                if (!string.IsNullOrEmpty(session.TherapistName))
                                {
                                    txt.Span($"  ·  with {session.TherapistName}")
                                        .FontSize(9.5f)
                                        .FontColor("#B5A898");
                                }
                            });

                        // Divider
                        col.Item()
                            .PaddingVertical(16)
                            .LineHorizontal(0.5f)
                            .LineColor("#E8C4A8");

                        // ── Section: Overview ──
                        if (includeBlocks.Contains("summary") &&
                            !string.IsNullOrWhiteSpace(summary.OneSentenceSummary))
                        {
                            col.Item()
                                .PaddingBottom(20)
                                .Column(section =>
                                {
                                    section.Item()
                                        .PaddingBottom(6)
                                        .Text("Overview")
                                        .FontSize(7.5f)
                                        .LetterSpacing(0.12f)
                                        .FontColor("#B5A898");

                                    section.Item()
                                        .Text(summary.OneSentenceSummary)
                                        .FontSize(13)
                                        .Italic()
                                        .FontColor("#5C3D2E")
                                        .LineHeight(1.7f);
                                });

                            col.Item()
                                .PaddingBottom(20)
                                .LineHorizontal(0.5f)
                                .LineColor("#E8C4A8");
                        }

                        // ── Section: Main Themes ──
                        if (includeBlocks.Contains("themes") &&
                            !string.IsNullOrWhiteSpace(summary.MainThemes))
                        {
                            col.Item()
                                .PaddingBottom(20)
                                .Column(section =>
                                {
                                    section.Item()
                                        .PaddingBottom(8)
                                        .Text("Main Themes")
                                        .FontSize(7.5f)
                                        .LetterSpacing(0.12f)
                                        .FontColor("#B5A898");

                                    section.Item()
                                        .Text(summary.MainThemes)
                                        .FontColor("#5C3D2E");
                                });

                            col.Item()
                                .PaddingBottom(20)
                                .LineHorizontal(0.5f)
                                .LineColor("#E8C4A8");
                        }

                        // ── Section: Emotional Patterns ──
                        if (includeBlocks.Contains("emotions") &&
                            !string.IsNullOrWhiteSpace(summary.EmotionalPatterns))
                        {
                            col.Item()
                                .PaddingBottom(20)
                                .Column(section =>
                                {
                                    section.Item()
                                        .PaddingBottom(8)
                                        .Text("Emotional Patterns")
                                        .FontSize(7.5f)
                                        .LetterSpacing(0.12f)
                                        .FontColor("#B5A898");

                                    section.Item()
                                        .Text(summary.EmotionalPatterns)
                                        .FontColor("#5C3D2E");
                                });

                            col.Item()
                                .PaddingBottom(20)
                                .LineHorizontal(0.5f)
                                .LineColor("#E8C4A8");
                        }

                        // ── Section: Things to Discuss ──
                        if (includeBlocks.Contains("discuss") &&
                            !string.IsNullOrWhiteSpace(summary.ThingsToDiscuss))
                        {
                            col.Item()
                                .Column(section =>
                                {
                                    section.Item()
                                        .PaddingBottom(8)
                                        .Text("Things to Discuss")
                                        .FontSize(7.5f)
                                        .LetterSpacing(0.12f)
                                        .FontColor("#B5A898");

                                    section.Item()
                                        .Text(summary.ThingsToDiscuss)
                                        .FontColor("#5C3D2E");
                                });
                        }
                    });

                    // ── Footer ──
                    page.Footer()
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.Span("Generated by Roots & Wings  ·  ")
                                .FontSize(8)
                                .FontColor("#B5A898");
                            txt.Span(DateTime.Today.ToString("MMMM d, yyyy"))
                                .FontSize(8)
                                .FontColor("#B5A898");
                        });
                });
            }).GeneratePdf();
        }
    }
}