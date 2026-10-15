using System.Linq;
using QuietWitness.Dialogue;
using QuietWitness.Save;
using UnityEditor;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Clears what the first grey-box prototype left behind on Systems.prefab: the old newspaper, evidence board,
    // journal, archive, court and old map windows (their scripts are gone), the empty "Missing Script" slots,
    // the empty places in the clue lists, and the old Diner / Police Station scenes in the build list.
    // Safe to run again. Menu: Tools > The Quiet Witness > Setup > Cleanup: old prototype parts
    public static class CleanupSetup
    {
        private static readonly string[] OldWindows = { "Newspaper", "Board", "Journal", "Archive", "JournalToast", "Court", "Map" };

        [MenuItem("Tools/The Quiet Witness/Setup/Cleanup: old prototype parts")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cleanup", "Stop Play mode first.", "OK");
                return;
            }
            int removed = 0, missing = 0;
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var canvas = root.transform.Find("Canvas");
                if (canvas != null)
                    foreach (var name in OldWindows)
                    {
                        var old = canvas.Find(name);
                        if (old == null) continue;
                        Object.DestroyImmediate(old.gameObject);
                        removed++;
                    }
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

                foreach (var save in root.GetComponentsInChildren<SaveSystem>(true)) DropEmpty(save, "clues");
                foreach (var runner in root.GetComponentsInChildren<DialogueRunner>(true)) DropEmpty(runner, "clues");

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Only scenes that are still there stay in the build.
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(s => !string.IsNullOrEmpty(s.path) && System.IO.File.Exists(s.path)).ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Cleanup done: {removed} old windows and {missing} empty script slots removed from Systems.");
        }

        private static void DropEmpty(Object target, string listName)
        {
            var so = new SerializedObject(target);
            var list = so.FindProperty(listName);
            if (list == null || !list.isArray) return;
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    int size = list.arraySize;
                    list.DeleteArrayElementAtIndex(i);
                    if (list.arraySize == size) list.DeleteArrayElementAtIndex(i);   // a missing file: first emptied, then removed
                }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
