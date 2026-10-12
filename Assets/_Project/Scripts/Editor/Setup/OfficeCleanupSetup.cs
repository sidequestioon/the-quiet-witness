using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Removes the grey-box prototype from the office: the invisible zones without icons
    // (evidence board, phone book, coat rack, the right door with the map, the stranger, the bottle,
    // the coroner's report) and the test markers that went with them.
    // Only objects in the office scene are removed; the scripts stay in the project.
    // The map is opened from the cork board (Look_Map). Safe to run again.
    // Menu: Tools > The Quiet Witness > Setup > Office cleanup
    public static class OfficeCleanupSetup
    {
        private static readonly string[] Old =
        {
            "Map_Wall", "Board_Wall", "CoatRack", "Hotspot_PhoneBook", "Exit", "Npc_Stranger",
            "Hotspot_Bottle", "Hotspot_Report", "Lamp_Bottle",
            "Conclusion_BottleNotWeapon", "Unlock_BottleNotWeapon",
            "Test_FridayOnly", "Test_SaturdayOnly", "Avail_Friday", "Avail_Saturday",
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Office cleanup")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Office cleanup", "Stop Play mode first.", "OK");
                return;
            }
            if (!OpenScene(Scene("Office"))) return;

            int removed = 0;
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (System.Array.IndexOf(Old, root.name) < 0) continue;
                Debug.Log($"[Setup] Removed {root.name}");
                Object.DestroyImmediate(root);
                removed++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log($"[Setup] Office cleanup done: {removed} old object(s) removed.");
        }
    }
}
