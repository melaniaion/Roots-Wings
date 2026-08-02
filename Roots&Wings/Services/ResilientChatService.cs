using Roots_Wings.DTOs;
using Roots_Wings.Models;

namespace Roots_Wings.Services
{
    // Same decorator pattern as ResilientSummaryService, applied to the chat.
    // The controller depends only on IAiChatService and never learns that
    // two providers exist. Each turn is independent, so if the primary fails
    // on a given turn we simply try the fallback for that turn; the browser
    // still holds the whole conversation either way.
    public class ResilientChatService : IAiChatService
    {
        private readonly GroqChatService _primary;
        private readonly GeminiChatService _fallback;
        private readonly ILogger<ResilientChatService> _logger;

        public ResilientChatService(
            GroqChatService primary,
            GeminiChatService fallback,
            ILogger<ResilientChatService> logger)
        {
            _primary = primary;
            _fallback = fallback;
            _logger = logger;
        }

        public async Task<ChatReply> ContinueDialogueAsync(
            TherapySession session,
            TherapySession? previousSession,
            List<Goal> openGoals,
            List<ChatTurn> transcript)
        {
            try
            {
                return await _primary.ContinueDialogueAsync(
                    session, previousSession, openGoals, transcript);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary chat provider failed, falling back to the secondary provider.");
            }

            return await _fallback.ContinueDialogueAsync(
                session, previousSession, openGoals, transcript);
        }
    }
}
