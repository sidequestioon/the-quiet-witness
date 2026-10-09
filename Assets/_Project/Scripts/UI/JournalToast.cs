using System.Collections;
using System.Collections.Generic;
using QuietWitness.Core;
using TMPro;
using UnityEngine;

namespace QuietWitness.UI
{
    // Small "New clue" notice in the corner when a clue is found.
    public class JournalToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float showTime = 2.5f;
        [SerializeField] private float fadeTime = 0.25f;

        public static JournalToast Instance { get; private set; }

        private readonly Queue<string> queue = new Queue<string>();
        private GameState state;
        private bool playing;

        private void Start()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;

            state = GameState.Instance;
            if (state == null || state.transform.root != transform.root) { state = null; return; } // duplicate Systems
            Instance = this;
            state.ClueFound += OnClueFound;
        }

        private void OnDestroy()
        {
            if (state != null) state.ClueFound -= OnClueFound;
        }

        // Any short notice, e.g. "Game saved".
        public void Notify(string message)
        {
            queue.Enqueue(message);
            if (!playing) StartCoroutine(Play());
        }

        private void OnClueFound(ClueData clue)
        {
            string kind = clue.IsKey ? "Key evidence" : "New clue";
            queue.Enqueue($"{kind}: <b>{clue.Title}</b>\n<size=75%>[J] Case file</size>");
            if (!playing) StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            playing = true;
            while (queue.Count > 0)
            {
                text.text = queue.Dequeue();
                yield return Fade(0f, 1f);
                yield return new WaitForSecondsRealtime(showTime);
                yield return Fade(1f, 0f);
            }
            playing = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
