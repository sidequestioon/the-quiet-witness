using QuietWitness.Core;
using QuietWitness.Dialogue;
using QuietWitness.Save;
using UnityEditor;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // One click for save/load: adds SaveSystem to the Systems root and fills every clue list.
    // Menu: Tools > The Quiet Witness > Setup > Save and load
    public static class SaveSetup
    {
        [MenuItem("Tools/The Quiet Witness/Setup/Save and load")]
        private static void Run()
        {
            var allClues = FindAll<ClueData>();

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var save = root.GetComponent<SaveSystem>();
                if (save == null) save = root.AddComponent<SaveSystem>();
                var saveSo = new SerializedObject(save);
                SetObjectList(saveSo, "clues", allClues);
                saveSo.ApplyModifiedPropertiesWithoutUndo();

                // Ink's give_clue should know every clue too.
                var runner = root.GetComponent<DialogueRunner>();
                if (runner != null)
                {
                    var runnerSo = new SerializedObject(runner);
                    SetObjectList(runnerSo, "clues", allClues);
                    runnerSo.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"[Setup] Save and load done ({allClues.Length} clues). F5 saves, F9 loads.");
        }
    }
}
