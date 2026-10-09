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

        public bool IsTravelling { get; private set; }

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

        public void Travel(LocationData location)
        {
            if (IsTravelling || location == null) return;
            StartCoroutine(TravelRoutine(location));
        }

        private IEnumerator TravelRoutine(LocationData location)
        {
            IsTravelling = true;
            InputBlocker.Push();
            fader.blocksRaycasts = true;

            yield return Fade(0f, 1f);

            string time = TimeManager.Instance.Current != null
                ? TimeManager.Instance.Current.DisplayName
                : "";
            title.text = $"{location.DisplayName}\n<size=60%>{time}</size>";

            AsyncOperation load = SceneManager.LoadSceneAsync(location.SceneName);
            float shown = 0f;
            while (!load.isDone || shown < titleTime)
            {
                shown += Time.deltaTime;
                yield return null;
            }

            GameState.Instance.SetFlag(location.VisitedFlag);
            title.text = "";
            yield return Fade(1f, 0f);

            fader.blocksRaycasts = false;
            InputBlocker.Pop();
            IsTravelling = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / fadeTime)
            {
                fader.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            fader.alpha = to;
        }
    }
}
