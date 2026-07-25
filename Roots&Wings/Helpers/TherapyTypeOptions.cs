namespace Roots_Wings.Helpers
{
    public static class TherapyTypeOptions
    {

        public static readonly Dictionary<string, string> All = new()
        {
            { "CBT", "Cognitive Behavioural Therapy (CBT)" },
            { "DBT", "Dialectical Behaviour Therapy (DBT)" },
            { "ACT", "Acceptance & Commitment Therapy (ACT)" },
            { "IFS", "Internal Family Systems (IFS)" },
            { "PersonCentred", "Person-Centred / General" },
            { "Unsure", "Not sure" }
        };

        // The value we fall back to when nothing is chosen
        public const string DefaultCode = "Unsure";

        // Turns a stored code into its display label. If the code is unknown
        // (e.g. bad data), it shows the "Not sure" label rather than a raw code.
        public static string LabelFor(string? code)
        {
            if (!string.IsNullOrEmpty(code) && All.TryGetValue(code, out var label))
            {
                return label;
            }
            return All[DefaultCode];
        }

        // Used by the controller to reject any value that isn't one of ours.
        public static bool IsValid(string? code)
        {
            return !string.IsNullOrEmpty(code) && All.ContainsKey(code);
        }
    }
}