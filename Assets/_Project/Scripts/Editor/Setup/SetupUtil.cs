using System;
using System.Reflection;
using QuietWitness.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace QuietWitness.EditorTools
{
    // Helpers for one-click setup scripts: they do the Inspector work instead of clicking by hand.
    // Every helper skips work that is already done, so a setup can be run twice safely.
    internal static class SetupUtil
    {
        public const string ProjectRoot = "Assets/_Project";
        public const string SystemsPrefab = ProjectRoot + "/Prefabs/Systems.prefab";

        public static string Scene(string name) => $"{ProjectRoot}/Scenes/{name}.unity";

        // ---- Assets ----

        public static T LoadAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning($"[Setup] Not found: {path}");
            return asset;
        }

        // Creates a ScriptableObject asset (or returns the existing one) and fills it.
        public static T CreateAsset<T>(string path, Action<SerializedObject> fill) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[Setup] Already exists: {path}");
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            var so = new SerializedObject(asset);
            fill(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            Debug.Log($"[Setup] Created {path}");
            return asset;
        }

        public static void SetString(SerializedProperty parent, string field, string value) =>
            parent.FindPropertyRelative(field).stringValue = value;

        public static void SetFlags(SerializedProperty condition, params string[] flags)
        {
            var list = condition.FindPropertyRelative("requiredFlags");
            list.arraySize = flags.Length;
            for (int i = 0; i < flags.Length; i++) list.GetArrayElementAtIndex(i).stringValue = flags[i];
        }

        public static void SetClues(SerializedProperty condition, params ClueData[] clues)
        {
            var list = condition.FindPropertyRelative("requiredClues");
            list.arraySize = clues.Length;
            for (int i = 0; i < clues.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = clues[i];
        }

        public static void SetObjectList<T>(SerializedObject so, string field, T[] items) where T : Object
        {
            var list = so.FindProperty(field);
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        public static T[] FindAll<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var result = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return result;
        }

        // ---- Systems prefab ----

        // Opens Systems.prefab, runs the edit on its Canvas and saves it.
        public static void EditSystemsCanvas(Action<Canvas> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas == null) throw new Exception("No Canvas in Systems prefab.");
                edit(canvas);
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---- Scenes ----

        public static bool OpenScene(string path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return true;
        }

        public static void SaveOpenScene() => EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // Copies a scene object (e.g. Map_Wall) as a starting point for a new hotspot.
        public static GameObject CloneInScene(string sourceName, string newName, Vector3 position, Color color)
        {
            var existing = GameObject.Find(newName);
            if (existing != null)
            {
                Debug.Log($"[Setup] Already in scene: {newName}");
                return null;
            }

            var source = GameObject.Find(sourceName);
            if (source == null)
            {
                Debug.LogError($"[Setup] '{sourceName}' not found in the scene.");
                return null;
            }

            var clone = Object.Instantiate(source, source.transform.parent);
            clone.name = newName;
            clone.transform.position = position;
            var sprite = clone.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.color = color;
            Undo.RegisterCreatedObjectUndo(clone, "Setup " + newName);
            return clone;
        }

        public static void RemoveComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null) Object.DestroyImmediate(component);
        }

        public static void SetField(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Replaces all persistent listeners of a UnityEvent field with one call.
        public static void SetOnlyListener(Component owner, string eventField, UnityAction call)
        {
            var field = owner.GetType().GetField(eventField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null || !(field.GetValue(owner) is UnityEvent unityEvent))
            {
                Debug.LogError($"[Setup] No UnityEvent '{eventField}' on {owner.GetType().Name}.");
                return;
            }
            while (unityEvent.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(unityEvent, 0);
            UnityEventTools.AddPersistentListener(unityEvent, call);
            EditorUtility.SetDirty(owner);
        }
    }
}
