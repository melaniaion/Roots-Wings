using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    public interface IAiSummaryService
    {
        Task<SummaryResult> SummariseEntriesAsync(List<JournalEntry> entries, TherapySession session);
    }
}
