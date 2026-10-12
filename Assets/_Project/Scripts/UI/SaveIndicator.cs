using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // A small floppy disk in the corner while the game saves (autosave too):
    // fades in, its metal shutter slides back and forth as if writing, fades out.
    [RequireComponent(typeof(CanvasGroup))]
    public class SaveIndicator : MonoBehaviour
    {
        public static SaveIndicator Instance { get; private set; }

        [SerializeField] private Image image;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameTime = 0.12f;
        [SerializeField] private float showTime = 1.4f;
        [SerializeField] private float fadeTime = 0.25f;

        private CanvasGroup group;
        private Coroutine playing;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show()
        {
            if (Instance == null || !Instance.isActiveAndEnabled) return;
            if (Instance.playing != null) Instance.StopCoroutine(Instance.playing);
            Instance.playing = Instance.StartCoroutine(Instance.Play());
        }

        private IEnumerator Play()
        {
            float t = 0f, total = fadeTime + showTime + fadeTime;
            int frame = 0;
            float nextFrame = 0f;
            while (t < total)
            {
                group.alpha = t < fadeTime ? t / fadeTime
                            : t > fadeTime + showTime ? 1f - (t - fadeTime - showTime) / fadeTime
                            : 1f;
                if (frames != null && frames.Length > 0 && t >= nextFrame)
                {
                    // 0 1 2 1 0 1 2 ... : the shutter goes back and forth
                    int cycle = frames.Length * 2 - 2;
                    int i = cycle > 0 ? frame % cycle : 0;
                    image.sprite = frames[i < frames.Length ? i : cycle - i];
                    frame++;
                    nextFrame += frameTime;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            group.alpha = 0f;
            if (frames != null && frames.Length > 0) image.sprite = frames[0];
            playing = null;
        }
    }
}
