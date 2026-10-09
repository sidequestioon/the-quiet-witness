using System;
using System.Collections.Generic;

namespace QuietWitness.Save
{
    // Everything a save file holds. Written as JSON.
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAt;
        public string scene;
        public string sceneTitle;
        public string segment;
        public List<string> flags = new List<string>();
        public List<string> clues = new List<string>(); // clue ids, in the order found
        public string ink;                                // ink story state (JSON)
        public List<Line> dialogue = new List<Line>();

        [Serializable]
        public class Line
        {
            public string speaker;
            public string text;
        }
    }
}
