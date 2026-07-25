using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // This is the service the controller actually gets.
    // It holds the two real providers and tries them in order, so
    // that one provider being down does not break the feature.

    // The pattern is a decorator: it implements the same interface it wraps,
    // adding behaviour (here, resilience) without the caller knowing. The
    // controller keeps depending on IAiSummaryService and never learns that
    // more than one provider exists.
    public class ResilientSummaryService : IAiSummaryService
    {
        private readonly GroqSummaryService _primary;
        private readonly GeminiSummaryService _fallback;
        private readonly ILogger<ResilientSummaryService> _logger;

        public ResilientSummaryService(
            GroqSummaryService primary,
            GeminiSummaryService fallback,
            ILogger<ResilientSummaryService> logger)
        {
            _primary = primary;
            _fallback = fallback;
            _logger = logger;
        }

        public async Task<SummaryResult> SummariseEntriesAsync(
            List<JournalEntry> entries, TherapySession session)
        {
            try
            {
                return await _primary.SummariseEntriesAsync(entries, session);
            }
            catch (Exception ex)
            {
                // The primary failed. This is expected occasionally, so it is
                // logged as a warning rather than an error, and we carry on.
                _logger.LogWarning(ex, "Primary AI provider failed, falling back to the secondary provider.");
            }

            // Second attempt. If this also throws, the exception travels up to
            // the controller, which shows the user an error message.
            return await _fallback.SummariseEntriesAsync(entries, session);
        }
    }
}