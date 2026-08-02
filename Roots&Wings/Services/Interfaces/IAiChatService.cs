using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // Separate from IAiSummaryService because the task is different:
    // a multi turn dialogue rather than a one shot summary. Keeping them
    // apart means each stays simple and can be reasoned about on its own.
    public interface IAiChatService
    {
        // Given everything known about the session, the previous session,
        // the user's open goals, and the conversation so far, returns the
        // assistant's next reply (a question, or the closing with takeaways).
        Task<ChatReply> ContinueDialogueAsync(
            TherapySession session,
            TherapySession? previousSession,
            List<Goal> openGoals,
            List<ChatTurn> transcript);
    }
}
