using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace QuietWitness.EditorTools
{
    // Runs the Setup menu items by itself: after Unity has loaded new code, every step below whose version is
    // newer than the one already done in this project is run once, in this order. Nothing to click.
    // What has been done is kept in ProjectSettings/QuietWitnessAutoSetup.txt (commit it with the rest).
    // To run a step again by hand: the same Tools > The Quiet Witness > Setup menu as before.
    // Menu: Tools > The Quiet Witness > Setup > Run pending setups now
    [InitializeOnLoad]
    public static class AutoSetup
    {
        private const string StateFile = "ProjectSettings/QuietWitnessAutoSetup.txt";
        private const string Menu = "Tools/The Quiet Witness/Setup/";

        // id, version (raised when the step changes and has to run again), the menu item it runs.
        private static readonly (string id, int version, string menu)[] Steps =
        {
            ("map-tear", 1, Menu + "Map: tear off when leaving the office"),
            ("board-notes", 1, Menu + "Map: dots on the board's photos"),
            ("gazette", 1, Menu + "Map: the Gazette to read"),
            ("cleanup-prototype", 1, Menu + "Cleanup: old prototype parts"),
            ("controls-window", 1, Menu + "Prologue: controls window"),
            ("answering-machine", 1, Menu + "Office: answering machine"),
        };

        private static bool queued;

        static AutoSetup()
        {
            if (Application.isBatchMode) return;
            Queue();
        }

        private static void Queue()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += RunPending;
        }

        private static void RunPending()
        {
            queued = false;
            // Not while Unity is busy or the game is running: try again a moment later.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) Queue();
                return;
            }
            var done = Load();
            var ran = new List<string>();
            foreach (var step in Steps)
            {
                done.TryGetValue(step.id, out int have);
                if (have >= step.version) continue;
                try
                {
                    if (!EditorApplication.ExecuteMenuItem(step.menu))
                    {
                        Debug.LogWarning($"[Auto setup] Menu not found: {step.menu}");
                        continue;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Auto setup] {step.menu} failed: {e.Message}");
                    break;   // the next steps may need this one
                }
                done[step.id] = step.version;
                Save(done);
                ran.Add(step.menu.Substring(Menu.Length));
            }
            if (ran.Count > 0) Debug.Log("[Auto setup] Done by itself: " + string.Join(", ", ran));
        }

        [MenuItem(Menu + "Run pending setups now", priority = 0)]
        private static void RunNow() => RunPending();

        private static Dictionary<string, int> Load()
        {
            var d = new Dictionary<string, int>();
            if (!File.Exists(StateFile)) return d;
            foreach (var line in File.ReadAllLines(StateFile))
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int v)) d[parts[0].Trim()] = v;
            }
            return d;
        }

        private static void Save(Dictionary<string, int> d)
        {
            var lines = new List<string> { "# Setup steps already done in this project (Tools > The Quiet Witness > Setup). Kept by AutoSetup.cs." };
            foreach (var kv in d) lines.Add($"{kv.Key}={kv.Value}");
            File.WriteAllLines(StateFile, lines);
        }
    }
}
