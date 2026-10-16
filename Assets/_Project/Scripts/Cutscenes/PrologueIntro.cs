using System.Collections;
using QuietWitness.Audio;
using QuietWitness.Core;
using QuietWitness.Map;
using QuietWitness.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.Cutscenes
{
    // The opening of the game in the office:
    // the hero lies on the sofa and smokes - a knock - an envelope slides in under the door -
    // a short cut to black, he is up - then the player is free; the envelope waits on the floor.
    // Plays once (Done Flag); after that the office starts normally, with the envelope still
    // on the floor until it is picked up (Taken Flag).
    public class PrologueIntro : MonoBehaviour
    {
        [Header("Story flags")]
        [SerializeField] private string doneFlag = "prologue:knock";
        [SerializeField] private string takenFlag = "prologue:envelope";

        [Header("In the scene")]
        [SerializeField] private GameObject lyingHero;
        [Tooltip("Where he stands after getting up (only x is used).")]
        [SerializeField] private Transform getUpSpot;
        [SerializeField] private SpriteFlipbook envelopeSlide;
        [SerializeField] private GameObject envelopeHotspot;

        [Header("Timing and sound")]
        [SerializeField] private AudioClip knock;
        [Range(0f, 1f)]
        [SerializeField] private float knockVolume = 0.9f;
        [Tooltip("Seconds he lies there before the knock.")]
        [SerializeField] private float lieFor = 7f;
        [Tooltip("Seconds between the knock and the envelope.")]
        [SerializeField] private float envelopeDelay = 0.8f;
        [Tooltip("Seconds the envelope lies there before he gets up.")]
        [SerializeField] private float beforeGettingUp = 1.6f;

        [Header("What he says when he gets up")]
        [Tooltip("The id of a line in Hero Lines (Resources/HeroLines).")]
        [HeroLine]
        [SerializeField] private string getUpLine = "prologue.get_up";
        [Tooltip("Only used when Get Up Line is empty.")]
        [TextArea(2, 6)]
        [SerializeField] private string[] afterGettingUp = { "Nobody knocks on my door after nine. Nobody good." };
        // What he says when he picks up the envelope: Take_Envelope > Hotspot Actions > Line.

        [Header("The controls window")]
        [Tooltip("Seconds after he gets up (and starts talking) before the controls window comes up.")]
        [SerializeField] private float controlsDelay = 1f;

        private IEnumerator Start()
        {
            var state = GameState.Instance;
            var hero = Object.FindFirstObjectByType<PlayerMover>();
            if (state != null && state.HasFlag(doneFlag))
            {
                if (lyingHero != null) lyingHero.SetActive(false);
                bool taken = state.HasFlag(takenFlag);
                if (envelopeSlide != null)
                {
                    envelopeSlide.gameObject.SetActive(!taken);
                    if (!taken) envelopeSlide.ShowLast();
                }
                if (envelopeHotspot != null) envelopeHotspot.SetActive(!taken);
                yield break;
            }

            // He lies on the sofa: the walking hero is hidden, the camera looks at the sofa.
            var heroSprite = hero != null ? hero.GetComponentInChildren<SpriteRenderer>() : null;
            if (heroSprite != null) heroSprite.enabled = false;
            if (hero != null && getUpSpot != null)
            {
                var p = hero.transform.position;
                hero.transform.position = new Vector3(getUpSpot.position.x, p.y, p.z);
            }
            if (lyingHero != null) lyingHero.SetActive(true);
            if (envelopeSlide != null) envelopeSlide.gameObject.SetActive(false);
            if (envelopeHotspot != null) envelopeHotspot.SetActive(false);
            InputBlocker.Push();

            // E / Space / Enter skips the waiting (handy while testing).
            for (float t = 0f; t < lieFor && !SkipPressed(); t += Time.deltaTime) yield return null;
            Sfx.Play(knock, knockVolume);
            yield return new WaitForSeconds(envelopeDelay);
            if (envelopeSlide != null)
            {
                envelopeSlide.gameObject.SetActive(true);
                envelopeSlide.Play();
            }
            yield return new WaitForSeconds(beforeGettingUp);

            // A cut: he is up.
            var loader = SceneLoader.Instance;
            if (loader != null) yield return loader.FadeToBlack(0.35f);
            if (lyingHero != null) lyingHero.SetActive(false);
            if (heroSprite != null) heroSprite.enabled = true;
            yield return new WaitForSeconds(0.3f);
            if (loader != null) yield return loader.FadeFromBlack(0.5f);

            if (envelopeHotspot != null) envelopeHotspot.SetActive(true);
            state?.SetFlag(doneFlag);
            InputBlocker.Pop();
            var getUp = new HotspotAction { line = getUpLine, lines = afterGettingUp };
            if (getUp.HasSpeech) HeroSpeech.Say(getUp);

            // A moment after he starts talking: the controls, once (his line may run on behind it).
            yield return new WaitForSeconds(controlsDelay);
            if (UI.ControlsHint.Instance != null) UI.ControlsHint.Instance.ShowOnce();
        }

        private static bool SkipPressed()
        {
            var k = Keyboard.current;
            return k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
        }

        // The envelope's "Take" action (Hotspot Actions > On Chosen).
        public void PickUpEnvelope()
        {
            if (envelopeSlide != null) envelopeSlide.gameObject.SetActive(false);
            if (envelopeHotspot != null) envelopeHotspot.SetActive(false);
            GameState.Instance?.SetFlag(takenFlag);
            StartCoroutine(OpenEnvelope());
        }

        // When he has said his line, the close-up of the envelope opens.
        private IEnumerator OpenEnvelope()
        {
            yield return null;
            while (HeroSpeech.IsSpeaking) yield return null;
            var inspect = Object.FindFirstObjectByType<EnvelopeInspect>(FindObjectsInactive.Include);
            if (inspect != null) inspect.Open();
        }
    }
}
