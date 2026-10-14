using System.Linq;
using QuietWitness.Core;
using QuietWitness.Map;
using UnityEditor;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // After the envelope is opened, the map lets him go to one place only: the Riviera's street
    // (Downtown, lit on the map; the goal shows in the case file). Every other district is only to read.
    // Leaving the office asks first: "Am I done here?" (Town Map > Leaving for good).
    // Doesn't rebuild the map. Safe to run again.
    // Menu: Tools > The Quiet Witness > Setup > Map: Riviera after the envelope
    public static class MapLeaveSetup
    {
        private const string Opened = "prologue:envelope_opened";

        [MenuItem("Tools/The Quiet Witness/Setup/Map: Riviera after the envelope")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Map", "Stop Play mode first.", "OK");
                return;
            }
            HeroLinesSetup.Ensure("map.leave_office",
                "Am I done here? Once I walk out that door, I'm not coming back tonight.");

            // The Riviera opens as soon as the envelope is opened.
            var riviera = AssetDatabase.LoadAssetAtPath<LocationData>(ProjectRoot + "/Cases/Locations/Riviera.asset");
            if (riviera == null)
            {
                EditorUtility.DisplayDialog("Map", "No Riviera location yet: run Setup > Town map first.", "OK");
                return;
            }
            var so = new SerializedObject(riviera);
            var flags = so.FindProperty("unlockCondition").FindPropertyRelative("requiredFlags");
            flags.arraySize = 1;
            flags.GetArrayElementAtIndex(0).stringValue = Opened;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Its goal shows at the same time.
            var objectives = AssetDatabase.LoadAssetAtPath<ObjectiveList>(ProjectRoot + "/Cases/Objectives.asset");
            var goal = objectives != null ? objectives.goals.FirstOrDefault(g => g.id == "riviera") : null;
            if (goal != null)
            {
                goal.showWhen = Opened;
                EditorUtility.SetDirty(objectives);
            }

            // Downtown is lit on the map from then on (until he has been there).
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                foreach (var district in root.GetComponentsInChildren<MapDistrict>(true))
                {
                    var dso = new SerializedObject(district);
                    var location = dso.FindProperty("location").objectReferenceValue;
                    if (location != riviera) continue;
                    var light = dso.FindProperty("lightWhen");
                    light.arraySize = 1;
                    light.GetArrayElementAtIndex(0).stringValue = Opened;
                    dso.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Map: the Riviera opens after the envelope; leaving the office asks first.");
        }
    }
}
