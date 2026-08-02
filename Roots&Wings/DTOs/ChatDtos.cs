namespace Roots_Wings.DTOs
{
    // One message in the reflection dialogue. Role is either "assistant"
    // (a question the app asked) or "user" (what the person answered).
    // This is the unit the browser holds and sends back each turn, so the
    // server never has to remember the conversation itself.
    public class ChatTurn
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    // What the server sends back after each step of the dialogue.
    public class ChatReply
    {
        // The assistant's next message (a question, or the closing message).
        public string Message { get; set; } = "";

        // True when the dialogue has reached its natural end. The frontend
        // then shows the "things worth carrying into your notes" step instead
        // of another answer box.
        public bool IsFinal { get; set; } = false;

        // Only populated on the final turn: short reflective takeaways drawn
        // from what the USER said, offered for them to pick from. The user
        // chooses which of these to append to their prep notes.
        public List<string> Takeaways { get; set; } = new List<string>();

        // Which provider produced this, for transparency and the demo.
        public string GeneratedBy { get; set; } = "";
    }
}
