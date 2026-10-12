using QuietWitness.Audio;
using QuietWitness.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Puts the sounds in place:
    //  - the town map: a sound when it opens and closes (Systems prefab, Map Panel);
    //  - office: rain and wind behind everything, the wall clock ticking quietly, steps on wood;
    //  - darkroom: steps on tiles, a quiet beep of some machine now and then.
    // The knock and the lock are imported and wait for the prologue and the locked drawer.
    // Each scene gets one "Sounds" object, made once: volumes you tune by hand are kept.
    // To get the defaults back, delete "Sounds" and run this again.
    // Menu: Tools > The Quiet Witness > Setup > Sounds
    public static class SoundSetup
    {
        private const string SfxFolder = ProjectRoot + "/Audio/SFX";
        private const string Ambience = ProjectRoot + "/Audio/Ambience";
        private const string Name = "Sounds";

        [MenuItem("Tools/The Quiet Witness/Setup/Sounds")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Sounds", "Stop Play mode first.", "OK");
                return;
            }

            // Ambience streams from disk instead of sitting in memory.
            var ambience = AssetImporter.GetAtPath($"{Ambience}/Ambient_RainWind_Loop.wav") as AudioImporter;
            if (ambience != null)
            {
                var settings = ambience.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                ambience.defaultSampleSettings = settings;
                ambience.SaveAndReimport();
            }

            // Map open / close.
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            var map = root.GetComponentInChildren<MapPanel>(true);
            if (map != null)
            {
                var so = new SerializedObject(map);
                so.FindProperty("openSound").objectReferenceValue = Clip($"{SfxFolder}/Map_Open.wav");
                so.FindProperty("closeSound").objectReferenceValue = Clip($"{SfxFolder}/Map_Close.wav");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            else Debug.LogWarning("[Setup] No Map Panel in Systems.prefab: the map stays silent.");
            PrefabUtility.UnloadPrefabContents(root);

            // Office.
            if (!OpenScene(Scene("Office"))) return;
            if (GameObject.Find(Name) == null)
            {
                var sounds = new GameObject(Name);
                Loop(sounds, "Rain", Clip($"{Ambience}/Ambient_RainWind_Loop.wav"), 0.35f);
                Interval(sounds, "Clock", new[] { Clip($"{SfxFolder}/Clock_Tick.wav"), Clip($"{SfxFolder}/Clock_Tock.wav") }, 0.1f,
                         new Vector2(1f, 1f), 0f);
                Steps(sounds, Clip($"{SfxFolder}/Step_Wood.wav"), 0.3f, 0.42f);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                SaveOpenScene();
            }

            // Darkroom.
            if (System.IO.File.Exists(Scene("Darkroom")))
            {
                if (!OpenScene(Scene("Darkroom"))) return;
                if (GameObject.Find(Name) == null)
                {
                    var sounds = new GameObject(Name);
                    Steps(sounds, Clip($"{SfxFolder}/Step_Tile.wav"), 0.35f, 0.42f);
                    Interval(sounds, "Beep", new[] { Clip($"{SfxFolder}/Darkroom_Beep.wav") }, 0.5f, new Vector2(4f, 9f), 0.05f);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    SaveOpenScene();
                }
            }

            Debug.Log("[Setup] Sounds done. Play the office: rain, clock, steps; open the map (from the cork board).");
        }

        private static AudioClip Clip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[Setup] Not found: {path}");
            return clip;
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<AudioSource>().playOnAwake = false;
            return go;
        }

        private static void Loop(GameObject parent, string name, AudioClip clip, float volume)
        {
            var loop = Child(parent, name).AddComponent<AmbientLoop>();
            var so = new SerializedObject(loop);
            so.FindProperty("clip").objectReferenceValue = clip;
            so.FindProperty("volume").floatValue = volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Interval(GameObject parent, string name, AudioClip[] clips, float volume, Vector2 interval, float pitch)
        {
            var sound = Child(parent, name).AddComponent<IntervalSound>();
            var so = new SerializedObject(sound);
            SetObjectList(so, "clips", clips);
            so.FindProperty("volume").floatValue = volume;
            so.FindProperty("interval").vector2Value = interval;
            so.FindProperty("pitchVariation").floatValue = pitch;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Steps(GameObject parent, AudioClip clip, float volume, float stepTime)
        {
            var steps = Child(parent, "Steps").AddComponent<FootstepSounds>();
            var so = new SerializedObject(steps);
            SetObjectList(so, "steps", new[] { clip });
            so.FindProperty("volume").floatValue = volume;
            so.FindProperty("stepTime").floatValue = stepTime;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
