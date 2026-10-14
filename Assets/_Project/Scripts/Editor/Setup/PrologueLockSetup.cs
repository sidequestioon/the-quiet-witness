using QuietWitness.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The office during the prologue: nothing to look at, no doors, no map, until the envelope is opened.
    // Only Take_Envelope stays. Safe to run again.
    // Menu: Tools > The Quiet Witness > Setup > Prologue lock
    public static class PrologueLockSetup
    {
        [MenuItem("Tools/The Quiet Witness/Setup/Prologue lock")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Prologue lock", "Stop Play mode first.", "OK");
                return;
            }
            if (!OpenScene(Scene("Office"))) return;
            var prologue = GameObject.Find("Prologue");
            if (prologue == null)
            {
                EditorUtility.DisplayDialog("Prologue lock", "No Prologue in the office: run Setup > Prologue first.", "OK");
                return;
            }
            if (!prologue.TryGetComponent<HotspotLock>(out var lockComp)) lockComp = prologue.AddComponent<HotspotLock>();
            var envelope = FindInactive("Take_Envelope");
            var so = new SerializedObject(lockComp);
            var list = so.FindProperty("except");
            list.arraySize = envelope != null ? 1 : 0;
            if (envelope != null) list.GetArrayElementAtIndex(0).objectReferenceValue = envelope;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log("[Setup] Prologue lock done: until the envelope is opened, only the envelope can be used.");
        }

        private static GameObject FindInactive(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
