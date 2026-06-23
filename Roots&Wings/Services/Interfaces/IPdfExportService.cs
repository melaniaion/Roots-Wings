using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    public interface IPdfExportService
    {
        byte[] GenerateSummaryPdf(
            TherapySession session,
            SummaryResult summary,
            string[] includeBlocks);
    }
}