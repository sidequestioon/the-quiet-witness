using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // One line on the Objectives page (a template: Main, Step or Mini). Every goal on the page is a copy of it,
    // so change the template: its Text (font, size, colour; move its left edge for more indent), its Marker
    // (picture, colour, position) and the numbers below.
    public class ObjectiveRow : MonoBehaviour
    {
        public TMP_Text text;
        [Tooltip("The circle (main goal), the tick on the dashed line (step) or the dot (mini step).")]
        public Image marker;
        [Tooltip("Space under this line, before the next one (canvas units).")]
        public float spaceAfter = 16f;
        [Tooltip("Text colour once it is done (crossed out).")]
        public Color doneColor = new Color(0.96f, 0.94f, 0.91f, 0.35f);
        [Tooltip("Marker colour once it is done.")]
        public Color doneMarker = new Color(0.94f, 0.85f, 0.64f, 0.3f);
    }
}
