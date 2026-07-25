namespace Roots_Wings.DTOs
{
    public class SummaryResult
    {
        // The three fixed sections of the summary document.
        // Names match the therapeutic purpose of each part.

        // A short overview of the period. Kind of a recap.
        public string Overall { get; set; } = "";

        // The recurring themes across the entries. This is the substantial
        // part of the document and grows with how much the user wrote.
        public string WhatKeptComingUp { get; set; } = "";

        // suggestions for the session. This is the only section
        // shaped by the therapy type and the only one kept kinda
        // short so the agenda stays small enough for one session.
        public string MightBringUp { get; set; } = "";

        // Which provider actually produced this summary. Set by the service,
        // not by the model. Useful for showing the user where the text came
        // from (and chekcing the fallback)
        public string GeneratedBy { get; set; } = "";
    }
}