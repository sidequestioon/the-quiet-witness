using System;
using System.Collections;
using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuietWitness.Map
{
    // Travel: fade to black, title card, load the scene, fade in.
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] private CanvasGroup fader;
        [SerializeField] private TMP_Text title;
        [SerializeField] private float fadeTime = 0.6f;
        [SerializeField] private float titleTime = 1.2f;
        [Tooltip("Through a door to the next room: just a short fade, no title.")]
        [SerializeField] private float quickFadeTime = 0.25f;

        public bool IsTravelling { get; private set; }

        // Fired when a travel or a load has finished and the screen is visible again.
        public event Action Arrived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            fader.alpha = 0f;
            fader.blocksRaycasts = false;
            title.text = "";
        }

        public void Travel(LocationData location) => Travel(location, quick: false);

        // quick: a door to the next room, a short fade without the title card.
        public void Travel(LocationData location, bool quick)
        {
            if (IsTravelling || location == null) return;
            StartCoroutine(LoadRoutine(location.SceneName, location.DisplayName, null,
                () => GameState.Instance.SetFlag(location.VisitedFlag), quick));
        }

        // Loading a save: whileDark restores the state while the screen is black.
        public void LoadSaved(string sceneName, string displayName, Action whileDark)
        {
            if (IsTravelling || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(LoadRoutine(sceneName, displayName, whileDark, null));
        }

        private IEnumerator LoadRoutine(string sceneName, string displayName, Action whileDark, Action afterLoad,
                                        bool quick = false)
        {
            IsTravelling = true;
            InputBlocker.Push();
            fader.blocksRaycasts = true;
            float fade = quick ? quickFadeTime : fadeTime;

            yield return Fade(0f, 1f, fade);

            whileDark?.Invoke();

            string time = TimeManager.Instance.Current != null
                ? TimeManager.Instance.Current.DisplayName
                : "";
            if (!quick) title.text = $"{displayName}\n<size=60%>{time}</size>";
            float hold = quick ? 0f : titleTime;

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            float shown = 0f;
            while (!load.isDone || shown < hold)
            {
                shown += Time.deltaTime;
                yield return null;
            }

            afterLoad?.Invoke();
            title.text = "";
            yield return Fade(1f, 0f, fade);

            fader.blocksRaycasts = false;
            InputBlocker.Pop();
            IsTravelling = false;
            Arrived?.Invoke();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                fader.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            fader.alpha = to;
        }
    }
}
