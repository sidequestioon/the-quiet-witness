using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace QuietWitness.UI
{
    // Lays a line out as a neat block: all lines about the same length (no long line with one lonely word
    // under it), and no more than a few lines (a long text gets a wider plate instead of a tall column).
    // Used by every plate with the hero's words: the speech over his head, the envelope things, the close-ups.
    // Also types text in place: the letters appear where they will stay, the lines don't move.
    public static class TextBlock
    {
        private struct Key
        {
            public string text;
            public float max, hardMax, size;
            public int lines, chars;
        }

        private static readonly Dictionary<int, (Key key, float width)> cache = new Dictionary<int, (Key, float)>();

        // The width to wrap the text at.
        //  max      - the usual widest plate;
        //  maxLines - if the text needs more lines than this at max, the plate gets wider (up to hardMax);
        //  hardMax  - never wider than this (0 = max);
        //  charsPerLine - about this many letters per line: a longer text is broken into more lines
        //                 even if it would fit in one (0 = only as the width needs).
        public static float Width(TMP_Text t, string text, float max, int maxLines = 3, float hardMax = 0f, int charsPerLine = 0)
        {
            if (t == null || string.IsNullOrEmpty(text) || max <= 0f) return max;
            hardMax = Mathf.Max(max, hardMax);
            var key = new Key { text = text, max = max, hardMax = hardMax, size = t.fontSize, lines = maxLines, chars = charsPerLine };
            int id = t.GetInstanceID();
            if (cache.TryGetValue(id, out var hit) && hit.key.text == key.text && hit.key.max == key.max &&
                hit.key.hardMax == key.hardMax && hit.key.size == key.size && hit.key.lines == key.lines &&
                hit.key.chars == key.chars) return hit.width;

            // Too many lines at the usual width: widen until it fits in maxLines (or hardMax is reached).
            if (maxLines > 0 && hardMax > max && Lines(t, text, max) > maxLines)
            {
                float lo = max, hi = hardMax;
                for (int i = 0; i < 14; i++)
                {
                    float mid = (lo + hi) / 2f;
                    if (Lines(t, text, mid) <= maxLines) hi = mid;
                    else lo = mid;
                }
                max = hi;
            }
            // A long text in one long line: break it into about charsPerLine letters a line.
            else if (charsPerLine > 0)
            {
                int want = Mathf.CeilToInt(text.Length / (float)charsPerLine);
                if (maxLines > 0) want = Mathf.Min(want, maxLines);
                if (want > 1 && Lines(t, text, max) < want)
                {
                    float lo = 0f, hi = max;   // the widest width that still gives `want` lines
                    for (int i = 0; i < 14; i++)
                    {
                        float mid = (lo + hi) / 2f;
                        if (Lines(t, text, mid) >= want) lo = mid;
                        else hi = mid;
                    }
                    if (lo > 0f) max = lo;
                }
            }

            // How tall it is at that width; then the narrowest width that is still that tall: even lines.
            float tall = t.GetPreferredValues(text, max, 0f).y;
            float a = 0f, b = max;
            for (int i = 0; i < 14; i++)
            {
                float mid = (a + b) / 2f;
                if (t.GetPreferredValues(text, mid, 0f).y <= tall + 0.001f) b = mid;
                else a = mid;
            }
            float width = Mathf.Min(max, b * 1.02f);   // a hair of room so rounding can't add a line
            cache[id] = (key, width);
            return width;
        }

        private static int Lines(TMP_Text t, string text, float width)
        {
            float one = t.GetPreferredValues("Ag", 100000f, 0f).y;
            if (one <= 0f) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(t.GetPreferredValues(text, width, 0f).y / one));
        }

        // Types the first `count` letters of `full`: the rest is there but see-through, so every letter
        // is already in its final place (the text doesn't grow from the bottom or shift).
        public static void Reveal(TMP_Text t, string full, int count)
        {
            if (t == null) return;
            full ??= string.Empty;
            count = Mathf.Clamp(count, 0, full.Length);
            string shown = count >= full.Length ? full : full.Substring(0, count) + "<alpha=#00>" + full.Substring(count);
            t.richText = true;
            if (t.maxVisibleCharacters != 99999) t.maxVisibleCharacters = 99999;
            if (t.text != shown) t.text = shown;
        }
    }
}
