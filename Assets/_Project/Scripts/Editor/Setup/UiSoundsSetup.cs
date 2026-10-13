using QuietWitness.Audio;
using UnityEditor;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The small sounds file: Resources/UiSounds.asset (clicks, the dot, the inventory, the envelope).
    // Fills only empty slots, so clips and volumes you changed stay as they are.
    // Menu: Tools > The Quiet Witness > Setup > UI sounds
    public static class UiSoundsSetup
    {
        private const string AssetPath = ProjectRoot + "/Resources/UiSounds.asset";
        private const string Sfx = ProjectRoot + "/Audio/SFX/";

        [MenuItem("Tools/The Quiet Witness/Setup/UI sounds")]
        private static void Run()
        {
            if (!AssetDatabase.IsValidFolder(ProjectRoot + "/Resources")) AssetDatabase.CreateFolder(ProjectRoot, "Resources");
            var asset = AssetDatabase.LoadAssetAtPath<UiSounds>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UiSounds>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            Fill(asset.click, "UI_Click.wav", 0.35f);
            Fill(asset.page, "UI_Page.wav", 0.4f);
            Fill(asset.envelopeTear, "Envelope_Tear.wav", 0.8f);
            Fill(asset.envelopeItems, "Envelope_ItemsOut.wav", 0.6f);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[Setup] UI sounds: {AssetPath}. Swap clips or volumes there.");
        }

        private static void Fill(UiSounds.Sound sound, string file, float volume)
        {
            if (sound.clip != null) return;
            sound.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + file);
            sound.volume = volume;
            if (sound.clip == null) Debug.LogWarning($"[Setup] Not found: {Sfx}{file}");
        }
    }
}
