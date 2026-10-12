using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.World
{
    // Everything the hero says out loud, in one file: Assets/_Project/Resources/HeroLines.asset.
    // Every line has an id (e.g. prologue.get_up, office.look_aquarium). Hotspots (Hotspot Actions > Line)
    // and the prologue point at a line by its id, so the text can be changed here at any time
    // (or right under the Line field of a hotspot) without touching code. Setup never overwrites it.
    [CreateAssetMenu(fileName = "HeroLines", menuName = "The Quiet Witness/Hero Lines")]
    public class HeroLines : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            [Tooltip("Name of the line. Hotspots use it, so better not to rename it once it is in use.")]
            public string id;
            [Tooltip("What he says. Enter starts a new line on the plate.")]
            [TextArea(2, 8)]
            public string text;
            [Tooltip("Widest the plate may get for this line, in pixels. 0 = the usual width (Hero Speech > Max Width).")]
            public int plateWidth;
        }

        public const string ResourceName = "HeroLines";

        public List<Line> lines = new List<Line>();

        private static HeroLines loaded;

        public static HeroLines Get()
        {
            if (loaded == null) loaded = Resources.Load<HeroLines>(ResourceName);
            return loaded;
        }

        public static Line Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var all = Get();
            if (all == null) return null;
            id = id.Trim();
            foreach (var line in all.lines)
                if (line != null && line.id == id) return line;
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => loaded = null;
    }

    // On a string field: pick a line from Hero Lines in a list, and edit its text right there.
    public class HeroLineAttribute : PropertyAttribute { }
}
