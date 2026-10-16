using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.World
{
    // Everything the hero says out loud, in one file: Assets/_Project/Resources/HeroLines.asset.
    // Every line has an id (e.g. prologue.get_up, office.look_aquarium). Hotspots (Hotspot Actions > Line)
    // and the prologue point at a line by its id. Every plate lays the text out as a neat block by itself
    // (TextBlock), so there is no width to set. Setup never overwrites a line.
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

    // Marks a string field that holds the id of a line in Hero Lines.
    public class HeroLineAttribute : PropertyAttribute { }
}
