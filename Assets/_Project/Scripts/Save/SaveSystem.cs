using System;
using System.Collections.Generic;
using System.IO;
using QuietWitness.Core;
using QuietWitness.Dialogue;
using QuietWitness.Map;
using QuietWitness.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace QuietWitness.Save
{
    // Save and load: one slot, JSON in the player's data folder.
    // Autosaves after every trip and every time the time of day changes.
    // F5 = save now, F9 = load. Lives on the Systems root.
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Tooltip("Every clue in the game, to find them again by id. The setup script fills this.")]
        [SerializeField] private List<ClueData> clues = new List<ClueData>();
        [SerializeField] private bool autosave = true;

        private const string FileName = "save1.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public bool HasSave => File.Exists(SavePath);

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
        }

        private void Start()
        {
            if (Instance != this) return;
            if (SceneLoader.Instance != null) SceneLoader.Instance.Arrived += OnArrived;
            if (TimeManager.Instance != null) TimeManager.Instance.SegmentChanged += OnSegmentChanged;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (SceneLoader.Instance != null) SceneLoader.Instance.Arrived -= OnArrived;
            if (TimeManager.Instance != null) TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
        }

        private void Update()
        {
            if (Instance != this || Keyboard.current == null || InputBlocker.IsBlocked) return;
            if (Keyboard.current.f5Key.wasPressedThisFrame) Save();
            else if (Keyboard.current.f9Key.wasPressedThisFrame) Load();
        }

        private void OnArrived() { if (autosave) Save(quiet: true); }
        private void OnSegmentChanged(TimeSegment _) { if (autosave) Save(quiet: true); }

        // ---- Save ----

        public void Save(bool quiet = false)
        {
            var state = GameState.Instance;
            if (state == null) return;

            var data = new SaveData
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                scene = SceneManager.GetActiveScene().name,
                sceneTitle = SceneManager.GetActiveScene().name,
                segment = TimeManager.Instance != null && TimeManager.Instance.Current != null
                    ? TimeManager.Instance.Current.Id
                    : null,
                flags = new List<string>(state.Flags),
            };
            foreach (var clue in state.FoundClues) data.clues.Add(clue.Id);

            var runner = DialogueRunner.Instance;
            if (runner != null)
            {
                data.ink = runner.SaveInkState();
                foreach (var line in runner.History)
                    data.dialogue.Add(new SaveData.Line { speaker = line.Speaker, text = line.Text });
            }

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
                Debug.Log($"[Save] Saved to {SavePath}");
                if (JournalToast.Instance != null)
                    JournalToast.Instance.Notify(quiet ? "<size=80%>Autosaved</size>" : "Game saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Could not save: {e.Message}");
            }
        }

        // ---- Load ----

        public void Load()
        {
            if (!HasSave)
            {
                if (JournalToast.Instance != null) JournalToast.Instance.Notify("No save yet");
                return;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Could not read the save: {e.Message}");
                return;
            }
            if (data == null || string.IsNullOrEmpty(data.scene)) return;

            // The state is restored while the screen is black, then the scene loads.
            SceneLoader.Instance.LoadSaved(data.scene, data.sceneTitle, () => Apply(data));
        }

        private void Apply(SaveData data)
        {
            var found = new List<ClueData>();
            foreach (var id in data.clues)
            {
                var clue = clues.Find(c => c != null && c.Id == id);
                if (clue != null) found.Add(clue);
                else Debug.LogWarning($"[Save] Unknown clue id '{id}' (add it to SaveSystem > Clues).");
            }
            GameState.Instance.Restore(data.flags, found);

            var time = TimeManager.Instance;
            if (time != null && !string.IsNullOrEmpty(data.segment))
                time.Restore(time.FindSegment(data.segment));

            var runner = DialogueRunner.Instance;
            if (runner != null)
            {
                runner.LoadInkState(data.ink);
                var lines = new List<DialogueLine>();
                foreach (var line in data.dialogue) lines.Add(new DialogueLine(line.speaker, line.text));
                runner.RestoreHistory(lines);
            }

            Debug.Log($"[Save] Loaded save from {data.savedAt}.");
        }

#if UNITY_EDITOR
        [ContextMenu("Collect all clues")]
        private void CollectAllClues()
        {
            clues.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ClueData"))
                clues.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<ClueData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Delete save file")]
        private void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            Debug.Log("[Save] Save file deleted.");
        }
#endif
    }
}
