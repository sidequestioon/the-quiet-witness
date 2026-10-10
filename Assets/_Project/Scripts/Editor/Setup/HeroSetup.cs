using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static QuietWitness.EditorTools.SetupUtil;
using Object = UnityEngine.Object;

namespace QuietWitness.EditorTools
{
    // One click: the hero drawn in Aseprite replaces the grey box.
    // For every Hero_<anim>.json in Art/Characters/Hero (Aseprite: Export Sprite Sheet, JSON Data, Array):
    //   slices Hero_<anim>.png by the rects in the json (pivot at the feet),
    //   makes Hero_<Anim>.anim with the frame durations set in Aseprite.
    // Then puts the idle animation on the Investigator prefab and sets every interior's camera:
    // Pixel Perfect 240x135 (close, like Backbone), following the hero inside the room.
    // Run again after every new export: sprite ids are kept, so nothing that uses them breaks.
    // Menu: Tools > The Quiet Witness > Setup > Hero sprite
    public static class HeroSetup
    {
        private const string HeroFolder = ProjectRoot + "/Art/Characters/Hero";
        private const string Prefix = "Hero_";
        private const string DefaultAnimation = "idle";
        private const string Controller = HeroFolder + "/Hero.controller";
        private const string StillClip = HeroFolder + "/Hero_Still.anim";
        // Set by PlayerMover while the hero walks.
        private const string MovingParameter = "Moving";
        private const string InvestigatorPrefab = ProjectRoot + "/Prefabs/Investigator.prefab";
        // Key resolution of the clips: 100 = 10 ms, the same step Aseprite durations use.
        private const float ClipFrameRate = 100f;
        // Drawn on top of the grey floor and props.
        private const int SortingOrder = 10;

        [MenuItem("Tools/The Quiet Witness/Setup/Hero sprite")]
        private static void Run()
        {
            // Scenes can't be opened and saved while the game is running.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Hero sprite", "Stop Play mode first, then run Setup again.", "OK");
                return;
            }

            var jsons = Directory.Exists(HeroFolder)
                ? Directory.GetFiles(HeroFolder, Prefix + "*.json").Select(p => p.Replace('\\', '/')).ToList()
                : new List<string>();
            if (jsons.Count == 0)
            {
                EditorUtility.DisplayDialog("Hero sprite",
                    $"No {Prefix}*.json in {HeroFolder}.\nAseprite: File > Export Sprite Sheet, tick JSON Data (Array).", "OK");
                return;
            }

            AssetDatabase.Refresh();
            var clips = new Dictionary<string, (AnimationClip clip, Sprite first)>();
            foreach (var json in jsons)
            {
                string animName = Path.GetFileNameWithoutExtension(json).Substring(Prefix.Length).ToLowerInvariant();
                var result = ImportAnimation(json, animName);
                if (result.clip != null) clips[animName] = result;
            }

            if (!clips.TryGetValue(DefaultAnimation, out var idle))
            {
                EditorUtility.DisplayDialog("Hero sprite", $"No {Prefix}{DefaultAnimation}.json, the hero needs an idle animation.", "OK");
                return;
            }

            // Walking: the walk animation once it is drawn (Hero_walk.json), until then the first idle frame standing still.
            AnimationClip walk = clips.TryGetValue("walk", out var walkResult)
                ? walkResult.clip
                : MakeLoopClip(StillClip, new List<Sprite> { idle.first }, new[] { 1000 });
            var controller = BuildController(idle.clip, walk);

            UpdateInvestigator(idle.first, controller);
            foreach (var scene in new[] { "Office", "Diner", "PoliceStation" })
                AddPixelPerfectCamera(scene);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Hero sprite done: {string.Join(", ", clips.Keys)}. Press Play in Office.");
        }

        // ---- Aseprite json (Array format) ----

        [Serializable] private class AseSheet { public AseFrame[] frames; public AseMeta meta; }
        [Serializable] private class AseFrame { public string filename; public AseRect frame; public int duration; }
        [Serializable] private class AseRect { public int x, y, w, h; }
        [Serializable] private class AseMeta { public string image; public AseSize size; }
        [Serializable] private class AseSize { public int w, h; }

        private static (AnimationClip clip, Sprite first) ImportAnimation(string jsonPath, string animName)
        {
            AseSheet sheet = null;
            try { sheet = JsonUtility.FromJson<AseSheet>(File.ReadAllText(jsonPath)); }
            catch (Exception e) { Debug.LogError($"[Setup] Can't read {jsonPath}: {e.Message}"); }
            if (sheet?.frames == null || sheet.frames.Length == 0 || sheet.meta == null)
            {
                Debug.LogError($"[Setup] {jsonPath}: no frames. In Aseprite export JSON Data as Array, not Hash.");
                return default;
            }

            string imagePath = Path.GetDirectoryName(jsonPath).Replace('\\', '/') + "/" + sheet.meta.image;
            if (!File.Exists(imagePath))
            {
                Debug.LogError($"[Setup] {jsonPath} points to {imagePath}, which is not there.");
                return default;
            }

            string baseName = "hero_" + animName;
            var sprites = SliceSheet(imagePath, baseName, sheet);
            if (sprites.Count != sheet.frames.Length)
            {
                Debug.LogError($"[Setup] {imagePath}: expected {sheet.frames.Length} sprites, got {sprites.Count}.");
                return default;
            }

            string clipPath = $"{HeroFolder}/Hero_{char.ToUpperInvariant(animName[0])}{animName.Substring(1)}.anim";
            var clip = MakeLoopClip(clipPath, sprites, sheet.frames.Select(f => f.duration).ToArray());
            return (clip, sprites[0]);
        }

        // ---- Sprites ----

        private static List<Sprite> SliceSheet(string path, string baseName, AseSheet sheet)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path);
                importer = (TextureImporter)AssetImporter.GetAtPath(path);
            }
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.SaveAndReimport();
            }

            int textureHeight = sheet.meta.size != null && sheet.meta.size.h > 0
                ? sheet.meta.size.h
                : AssetDatabase.LoadAssetAtPath<Texture2D>(path).height;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Keep the ids of sprites that already exist, so the clip and the prefab keep pointing at them.
            var oldIds = provider.GetSpriteRects()
                .GroupBy(r => r.name)
                .ToDictionary(g => g.Key, g => g.First().spriteID);

            var rects = new List<SpriteRect>();
            var names = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < sheet.frames.Length; i++)
            {
                var f = sheet.frames[i].frame;
                string name = $"{baseName}_{i}";
                var spriteRect = new SpriteRect
                {
                    name = name,
                    // Aseprite counts y from the top, Unity from the bottom.
                    rect = new Rect(f.x, textureHeight - f.y - f.h, f.w, f.h),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = oldIds.TryGetValue(name, out var id) ? id : GUID.Generate(),
                };
                rects.Add(spriteRect);
                names.Add(new SpriteNameFileIdPair(name, spriteRect.spriteID));
            }

            provider.SetSpriteRects(rects.ToArray());
            var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProvider != null) nameProvider.SetNameFileIdPairs(names);
            provider.Apply();
            importer.SaveAndReimport();
            ApplyFeetPivot(path);

            return LoadSprites(path, baseName);
        }

        // Every frame's pivot at the bottom center (the feet), so frames line up on the floor.
        private static void ApplyFeetPivot(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var so = new SerializedObject(importer);
            var sprites = so.FindProperty("m_SpriteSheet.m_Sprites");
            bool changed = false;
            for (int i = 0; i < sprites.arraySize; i++)
            {
                var sprite = sprites.GetArrayElementAtIndex(i);
                var alignment = sprite.FindPropertyRelative("m_Alignment");
                var pivot = sprite.FindPropertyRelative("m_Pivot");
                if (alignment.intValue == (int)SpriteAlignment.BottomCenter && pivot.vector2Value == new Vector2(0.5f, 0f)) continue;
                alignment.intValue = (int)SpriteAlignment.BottomCenter;
                pivot.vector2Value = new Vector2(0.5f, 0f);
                changed = true;
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }

        private static List<Sprite> LoadSprites(string path, string baseName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .Where(s => s.name.StartsWith(baseName + "_"))
                .OrderBy(s => int.TryParse(s.name.Substring(baseName.Length + 1), out int n) ? n : 0)
                .ToList();
        }

        // ---- Animation ----

        private static AnimationClip MakeLoopClip(string path, List<Sprite> sprites, int[] durationsMs)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = ClipFrameRate;

            // One key per frame at the time Aseprite shows it, plus a closing key so the last frame
            // lasts its full duration before the loop starts again.
            var keys = new ObjectReferenceKeyframe[sprites.Count + 1];
            float time = 0f;
            for (int i = 0; i < sprites.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = time, value = sprites[i] };
                time += Mathf.Max(10, durationsMs[i]) / 1000f;
            }
            keys[sprites.Count] = new ObjectReferenceKeyframe { time = time, value = sprites[sprites.Count - 1] };

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        // ---- Animator ----

        // Idle while standing, Walk while the Moving parameter is on. Rebuilt from scratch on every run.
        private static AnimatorController BuildController(AnimationClip idle, AnimationClip walk)
        {
            AssetDatabase.DeleteAsset(Controller);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            controller.AddParameter(MovingParameter, AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle");
            idleState.motion = idle;
            var walkState = machine.AddState("Walk");
            walkState.motion = walk;
            machine.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, MovingParameter);

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, MovingParameter);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ---- Investigator prefab ----

        private static void UpdateInvestigator(Sprite firstFrame, RuntimeAnimatorController controller)
        {
            var root = PrefabUtility.LoadPrefabContents(InvestigatorPrefab);
            try
            {
                var visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    // The grey box was a 1x1 sprite scaled on the root. Move the picture to a child
                    // with its pivot at the feet, and keep the collider the same size in the world.
                    var rootRenderer = root.GetComponent<SpriteRenderer>();
                    Vector3 scale = root.transform.localScale;

                    var go = new GameObject("Visual");
                    go.layer = root.layer;
                    visual = go.transform;
                    visual.SetParent(root.transform, false);

                    var renderer = go.AddComponent<SpriteRenderer>();
                    if (rootRenderer != null)
                    {
                        renderer.sharedMaterial = rootRenderer.sharedMaterial;
                        renderer.sortingLayerID = rootRenderer.sortingLayerID;
                        renderer.sortingOrder = rootRenderer.sortingOrder;
                        Object.DestroyImmediate(rootRenderer);
                    }

                    var box = root.GetComponent<BoxCollider2D>();
                    if (box != null)
                    {
                        box.size = Vector2.Scale(box.size, scale);
                        box.offset = Vector2.Scale(box.offset, scale);
                    }

                    root.transform.localScale = Vector3.one;
                    visual.localPosition = new Vector3(0f, -0.5f * scale.y, 0f); // feet where the box ended
                }

                var visualRenderer = visual.GetComponent<SpriteRenderer>();
                visualRenderer.sprite = firstFrame;
                visualRenderer.sortingOrder = SortingOrder;

                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                PrefabUtility.SaveAsPrefabAsset(root, InvestigatorPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---- Camera ----

        // Interiors are shot close, like Backbone: 240x135 pixels of art on screen,
        // the hero (80 px) is ~60% of the view. 1080p x8, 1440p x10 (shows 256x144), 4K x16.
        private const int InteriorWidth = 240;
        private const int InteriorHeight = 135;
        // Pixels of floor below the hero's feet at the bottom of the view.
        private const int FloorBelowFeet = 8;

        private static void AddPixelPerfectCamera(string sceneName)
        {
            if (!OpenScene(Scene(sceneName))) return;
            var cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
            {
                Debug.LogWarning($"[Setup] No Main Camera in {sceneName}.");
                return;
            }

            var ppc = cameraObject.GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = cameraObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = PixelArtImporter.PixelsPerUnit;
            ppc.refResolutionX = InteriorWidth;
            ppc.refResolutionY = InteriorHeight;
            // No snapping: the picture is drawn at screen resolution, so the camera glides smoothly
            // instead of jumping a whole art pixel (8 screen pixels at 1080p) at a time.
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.None;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            EditorUtility.SetDirty(ppc);

            // The room is as wide as the floor for now; later, as wide as the drawn background.
            var follow = cameraObject.GetComponent<QuietWitness.World.CameraFollow>();
            var floor = GameObject.Find("Floor");
            if (follow != null && floor != null && floor.TryGetComponent<Renderer>(out var floorRenderer))
            {
                Bounds room = floorRenderer.bounds;
                var so = new SerializedObject(follow);
                so.FindProperty("roomLeft").floatValue = room.min.x;
                so.FindProperty("roomRight").floatValue = room.max.x;
                so.FindProperty("viewBottom").floatValue = room.max.y - (float)FloorBelowFeet / PixelArtImporter.PixelsPerUnit;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Same framing in the editor as in Play.
                var pos = cameraObject.transform.position;
                pos.y = so.FindProperty("viewBottom").floatValue + InteriorHeight * 0.5f / PixelArtImporter.PixelsPerUnit;
                cameraObject.transform.position = pos;
            }
            else
            {
                Debug.LogWarning($"[Setup] {sceneName}: no CameraFollow or Floor, camera bounds not set.");
            }

            SaveOpenScene();
        }
    }
}
