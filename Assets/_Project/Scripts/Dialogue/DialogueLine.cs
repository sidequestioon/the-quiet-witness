using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuietWitness.Dialogue
{
    // One line of dialogue, already split into speaker and text.
    // In ink, write "Name: text" for a character and plain text for narration
    // or the investigator's thoughts. Tags like "#mood:angry" are kept for later.
    public class DialogueLine
    {
        // "Diner owner: Coffee?" -> speaker "Diner owner", text "Coffee?"
        private static readonly Regex SpeakerPrefix =
            new Regex(@"^([\p{L}][\p{L}\p{N} .'\-]{0,23}):\s+(.+)$", RegexOptions.Singleline);

        public string Speaker { get; }
        public string Text { get; }
        public IReadOnlyDictionary<string, string> Tags { get; }

        public bool IsNarration => string.IsNullOrEmpty(Speaker);

        public DialogueLine(string speaker, string text, Dictionary<string, string> tags = null)
        {
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            Tags = tags ?? new Dictionary<string, string>();
        }

        public static DialogueLine Parse(string raw, List<string> inkTags)
        {
            var tags = new Dictionary<string, string>();
            if (inkTags != null)
            {
                foreach (var tag in inkTags)
                {
                    int colon = tag.IndexOf(':');
                    string key = (colon < 0 ? tag : tag.Substring(0, colon)).Trim().ToLowerInvariant();
                    string value = colon < 0 ? string.Empty : tag.Substring(colon + 1).Trim();
                    if (key.Length > 0) tags[key] = value;
                }
            }

            string text = (raw ?? string.Empty).Trim();
            string speaker = string.Empty;

            var match = SpeakerPrefix.Match(text);
            if (match.Success)
            {
                speaker = match.Groups[1].Value.Trim();
                text = match.Groups[2].Value.Trim();
            }
            if (tags.TryGetValue("speaker", out var tagged)) speaker = tagged;

            return new DialogueLine(speaker, text, tags);
        }
    }
}
