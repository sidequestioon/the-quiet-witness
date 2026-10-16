using System.Collections;
using System.Collections.Generic;
using QuietWitness.Audio;
using QuietWitness.Core;
using QuietWitness.UI;
using UnityEngine;

namespace QuietWitness.World
{
    // The answering machine in the office: its little red light blinks while there's a message nobody has heard.
    // Listen (its Use action, Hotspot Steps) plays it: a beep, the message line by line at the bottom of the
    // screen (Voice Subtitles), a beep. The first time, the hero says something after it and Heard Flag is set
    // (it also shows the side goal in the case file). The message: the Message list below.
    public class AnsweringMachine : MonoBehaviour
    {
        [SerializeField] private List<VoiceLine> message = new List<VoiceLine>();
        [Tooltip("Set once the message has been heard.")]
        [SerializeField] private string heardFlag = "office:sal_heard";
        [Tooltip("What the hero says after hearing it the first time (from Hero Lines).")]
        [HeroLine]
        [SerializeField] private string afterFirstTime = "office.machine_after";

        [Header("The light")]
        [SerializeField] private SpriteRenderer led;
        [SerializeField] private Sprite ledOn;
        [SerializeField] private Sprite ledOff;
        [SerializeField] private float blinkTime = 0.6f;

        [Header("Sound")]
        [SerializeField] private AudioClip beep;
        [Range(0f, 1f)]
        [SerializeField] private float beepVolume = 0.5f;

        private bool playing;

        private bool Heard => GameState.Instance != null && GameState.Instance.HasFlag(heardFlag);

        private void Update()
        {
            if (led == null) return;
            bool on = !Heard && !playing && Mathf.Repeat(Time.time, blinkTime * 2f) < blinkTime;
            var s = on || playing ? ledOn : ledOff;
            if (led.sprite != s) led.sprite = s;
        }

        // Its Use action (Listen).
        public void Play()
        {
            if (playing || VoiceSubtitles.Instance == null) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            playing = true;
            InputBlocker.Push();
            Sfx.Play(beep, beepVolume);
            yield return new WaitForSeconds(0.5f);
            bool done = false;
            VoiceSubtitles.Instance.Play(message, () => done = true);
            while (!done) yield return null;
            Sfx.Play(beep, beepVolume);
            yield return new WaitForSeconds(0.3f);
            InputBlocker.Pop();
            playing = false;

            bool first = !Heard;
            GameState.Instance?.SetFlag(heardFlag);
            if (first && !string.IsNullOrWhiteSpace(afterFirstTime))
                HeroSpeech.Say(new HotspotAction { line = afterFirstTime });
        }
    }
}
