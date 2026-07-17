namespace Roots_Wings.Helpers
{
    // All timestamps are stored in UTC. This converts them to the user's local
    // time for display only — storage stays timezone-independent.
    public static class TimeHelper
    {
        // The zone the app displays in. Single-timezone by design; if this ever
        // becomes multi-user across regions, i might read it from ApplicationUser instead (for later)
        private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");

        // UTC (as stored) -> local (as shown).
        public static DateTime ToLocal(this DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
        }

        public static DateTime? ToLocal(this DateTime? utc)
        {
            return utc.HasValue ? utc.Value.ToLocal() : (DateTime?)null;
        }

        // Local (as typed into a form) -> UTC (as stored).
        public static DateTime ToUtc(this DateTime local)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);
        }
    }
}