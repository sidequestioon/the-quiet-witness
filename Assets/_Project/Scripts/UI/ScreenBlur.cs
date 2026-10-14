using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuietWitness.UI
{
    // The blurred room behind a close-up: one shared Volume, made on first use and kept between scenes.
    // Post-processing is switched on for the camera while the blur shows, and back off after.
    public static class ScreenBlur
    {
        private static Volume volume;
        private static Camera camera;
        private static bool cameraWasOn;

        public static void Set(VolumeProfile profile, float weight)
        {
            if (profile == null) return;
            if (volume == null)
            {
                var go = new GameObject("ScreenBlur");
                Object.DontDestroyOnLoad(go);
                volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 30f;
            }
            volume.sharedProfile = profile;
            volume.weight = weight;

            if (weight > 0f)
            {
                var cam = Camera.main;
                if (cam != null && cam != camera && cam.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                {
                    Restore();
                    camera = cam;
                    cameraWasOn = data.renderPostProcessing;
                    data.renderPostProcessing = true;
                }
            }
            else Restore();
        }

        private static void Restore()
        {
            if (camera != null && camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                data.renderPostProcessing = cameraWasOn;
            camera = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            volume = null;
            camera = null;
        }
    }
}
