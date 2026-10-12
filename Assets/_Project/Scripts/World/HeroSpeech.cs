using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.World
{
    // The hero's thoughts said out loud: text on a plate over his head, typed out letter by letter.
    // The text comes from Hero Lines (Resources/HeroLines) through a hotspot's Line, or from its Lines.
    // E / Space / Enter / click: show the whole text at once, then close.
    // He stands still while talking; A/D or the arrows close it and he walks off.
    //
    // The plate is three child objects: Speech (the text), Plate (9-sliced picture) and Tail (the
    // little triangle). Tick Preview to see them over the hero outside Play mode, then style them
    // on their own components: font, size and colour on Speech; picture, colour and material on
    // Plate and Tail. Made by Setup > Hero lines + speech plate.
    [ExecuteAlways]
    public class HeroSpeech : MonoBehaviour
    {
        [Header("The plate (style it on these objects)")]
        [Tooltip("The text: font, Font Size and colour are set on it.")]
        [SerializeField] private TextMeshPro text;
        [Tooltip("The plate: its picture (9-sliced), colour and material.")]
        [SerializeField] private SpriteRenderer plate;
        [Tooltip("The little triangle pointing at the hero's head.")]
        [SerializeField] private SpriteRenderer tail;

        [Header("Preview outside Play mode")]
        [Tooltip("Shows the plate with the sample text over the hero, in the Scene and Game views.")]
        [SerializeField] private bool preview;
        [TextArea(2, 5)]
        [SerializeField] private string previewText = "Nobody knocks on my door after nine. Nobody good.";
        [Tooltip("Plate width for the sample, in pixels. 0 = Max Width.")]
        [SerializeField] private int previewWidth;

        [Header("Size and place (can be tuned in Play mode and seen at once)")]
        [Tooltip("The plate never gets wider than this (pixels): longer text wraps onto more lines.")]
        [SerializeField] private int maxWidth = 130;
        [Tooltip("The plate is never narrower than this (pixels), even for a short line.")]
        [SerializeField] private int minWidth = 0;
        [Tooltip("Pixels of plate around the text: x = left/right, y = top/bottom.")]
        [SerializeField] private Vector2Int platePadding = new Vector2Int(4, 3);
        [Tooltip("Pixels between the top of the hero's head and the tail.")]
        [SerializeField] private int gapAboveHead = 4;
        [SerializeField] private float lettersPerSecond = 45f;
        [Tooltip("Off: all lines of a hotspot are shown together on one plate. On: one line per E press.")]
        [SerializeField] private bool oneLineAtATime = false;

        [Header("Only used when the plate objects above are empty")]
        [SerializeField] private float fontSize = 1.2f;
        [SerializeField] private Color textColor = new Color(0.96f, 0.94f, 0.91f);
        [SerializeField] private Color plateColor = new Color(0.055f, 0.05f, 0.063f, 1f);
        [Tooltip("The hero is 10: the text is drawn over him.")]
        [SerializeField] private int sortingOrder = 30;
        [Tooltip("Unlit, so the plate stays black in any room light.")]
        [SerializeField] private Material plateMaterial;

        private const float PixelsPerUnit = 36f;
        private const float FadeTime = 0.15f;

        private static HeroSpeech instance;
        private static Sprite fallbackPlate;
        private static Sprite fallbackTail;

        private Bounds lineBounds;           // the whole current line, in the text's local space
        private float measuredSize;
        private int measuredWidth;
        private string[] lines;
        private int lineIndex;
        private float shownLetters;
        private bool speaking;
        private float fade;
        private int openedFrame;
        private Transform hero;
        private Renderer heroRenderer;
        private float textAlpha = 1f, plateAlpha = 1f, tailAlpha = 1f;
        private HotspotAction source;   // where the lines came from (for its plate width)

        public static bool IsSpeaking => instance != null && instance.speaking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        // Shows the lines over the hero. False if there is no HeroSpeech in the scene.
        public static bool Say(string[] say) => Say(say, null);

        // A hotspot's line (from Hero Lines) or its Lines, with its plate width.
        public static bool Say(HotspotAction action) => action != null && Say(action.SaidLines, action);

        private static bool Say(string[] say, HotspotAction from)
        {
            if (instance == null) instance = FindFirstObjectByType<HeroSpeech>();
            if (instance == null || say == null || say.Length == 0) return false;
            instance.source = from;
            instance.Begin(say);
            return true;
        }

        // This text's widest plate: the line's own, or the usual one.
        private int MaxWidth
        {
            get
            {
                if (source != null && source.SaidWidth > 0) return source.SaidWidth;
                if (!Application.isPlaying && previewWidth > 0) return previewWidth;
                return maxWidth;
            }
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            instance = this;
            if (text == null || plate == null || tail == null) BuildFallback();
            // The alpha set on the objects is the full strength; the plate fades in and out to it.
            textAlpha = text.color.a;
            plateAlpha = plate.color.a;
            tailAlpha = tail.color.a;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.text = "";
            Show(false);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // Older scenes without the plate objects: made in code like before.
        private void BuildFallback()
        {
            if (text == null)
            {
                var go = new GameObject("Speech");
                go.transform.SetParent(transform, false);
                text = go.AddComponent<TextMeshPro>();
                var prompt = GetComponent<InteractionPrompt>();
                if (prompt != null && prompt.Font != null) text.font = prompt.Font;
                text.fontSize = fontSize;
                text.color = textColor;
                text.alignment = TextAlignmentOptions.Top;
                text.rectTransform.pivot = new Vector2(0.5f, 0f);
                text.sortingOrder = sortingOrder;
            }
            if (plate == null)
            {
                if (fallbackPlate == null)
                {
                    var tex = new Texture2D(3, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    for (int y = 0; y < 3; y++)
                        for (int x = 0; x < 3; x++)
                            tex.SetPixel(x, y, (x != 1 && y != 1) ? Color.clear : Color.white);
                    tex.Apply();
                    fallbackPlate = Sprite.Create(tex, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0,
                                                  SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
                }
                plate = MakeRenderer("Plate", fallbackPlate);
                plate.drawMode = SpriteDrawMode.Sliced;
            }
            if (tail == null)
            {
                if (fallbackTail == null)
                {
                    var tex = new Texture2D(5, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    for (int y = 0; y < 3; y++)
                        for (int x = 0; x < 5; x++)
                            tex.SetPixel(x, y, Mathf.Abs(x - 2) <= y ? Color.white : Color.clear);
                    tex.Apply();
                    fallbackTail = Sprite.Create(tex, new Rect(0, 0, 5, 3), Vector2.zero, PixelsPerUnit);
                }
                tail = MakeRenderer("Tail", fallbackTail);
            }
        }

        private SpriteRenderer MakeRenderer(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            if (plateMaterial != null) r.sharedMaterial = plateMaterial;
            r.sprite = sprite;
            r.color = plateColor;
            r.sortingOrder = sortingOrder - 1;
            return r;
        }

        private void Show(bool on)
        {
            if (text != null && text.enabled != on) text.enabled = on;
            if (plate != null && plate.enabled != on) plate.enabled = on;
            if (tail != null && tail.enabled != on) tail.enabled = on;
        }

        private void Begin(string[] say)
        {
            if (!speaking) InputBlocker.Push();
            // "\\n" typed by hand also starts a new line (Enter in the text box does it too).
            var cleaned = new string[say.Length];
            for (int i = 0; i < say.Length; i++) cleaned[i] = say[i].Replace("\\n", "\n");
            lines = oneLineAtATime ? cleaned : new[] { string.Join(" ", cleaned) };
            lineIndex = 0;
            speaking = true;
            openedFrame = Time.frameCount;
            FindHero();
            ShowLine();
        }

        private void FindHero()
        {
            if (hero != null) return;
            var mover = FindFirstObjectByType<PlayerMover>();
            if (mover == null) return;
            hero = mover.transform;
            heroRenderer = mover.GetComponentInChildren<SpriteRenderer>();
        }

        private void ShowLine()
        {
            text.text = lines[lineIndex];
            shownLetters = 0f;
            // Switched on before measuring: a switched-off text keeps its old size.
            Show(true);
            Measure();
            Place();   // right away, so the first frame is already in the right spot
        }

        // Size of the whole line, so the plate is ready before the letters come.
        private void Measure()
        {
            text.rectTransform.sizeDelta = new Vector2(MaxWidth / PixelsPerUnit, 1f);
            text.maxVisibleCharacters = 99999;
            text.ForceMeshUpdate(true, true);
            lineBounds = text.textBounds;
            if (lineBounds.size.x <= 0f)   // font not ready yet: fall back to its preferred size
            {
                Vector2 size = text.GetPreferredValues(text.text, MaxWidth / PixelsPerUnit, 0f);
                Rect r = text.rectTransform.rect;
                lineBounds = new Bounds(new Vector3(r.center.x, r.yMax - size.y / 2f, 0f), new Vector3(size.x, size.y, 0f));
            }
            if (Application.isPlaying) text.maxVisibleCharacters = Mathf.FloorToInt(shownLetters);
            measuredSize = text.fontSize;
            measuredWidth = MaxWidth;
        }

        private void End()
        {
            if (!speaking) return;
            speaking = false;
            InputBlocker.Pop();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                EditorPreview();
                return;
            }

            fade = Mathf.MoveTowards(fade, speaking ? 1f : 0f, Time.deltaTime / FadeTime);
            if (!speaking && fade <= 0f)
            {
                Show(false);
                return;
            }

            // Only the alpha fades: colours changed on the objects in Play mode show at once.
            text.alpha = textAlpha * fade;
            var pc = plate.color; pc.a = plateAlpha * fade; plate.color = pc;
            var tc = tail.color; tc.a = tailAlpha * fade; tail.color = tc;

            if (!Mathf.Approximately(measuredSize, text.fontSize) || measuredWidth != MaxWidth) Measure();

            if (speaking)
            {
                shownLetters += lettersPerSecond * Time.deltaTime;
                text.maxVisibleCharacters = Mathf.FloorToInt(shownLetters);
                if (Time.frameCount != openedFrame)
                {
                    if (WalkPressed()) End();          // walking away closes it, and he can move at once
                    else if (Pressed()) Advance();
                }
            }
        }

        // Outside Play mode: the sample text on the plate over the hero, while Preview is on.
        private void EditorPreview()
        {
            if (text == null || plate == null || tail == null) return;
            bool on = preview && isActiveAndEnabled;
            Show(on);
            if (!on) return;
            source = null;
            string sample = string.IsNullOrEmpty(previewText) ? " " : previewText.Replace("\\n", "\n");
            if (text.text != sample) text.text = sample;
            text.textWrappingMode = TextWrappingModes.Normal;
            Measure();
            FindHero();
            Place();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif

        private void LateUpdate()
        {
            if (Application.isPlaying && (speaking || fade > 0f)) Place();
        }

        // Over the hero's head, following him; the plate on whole pixels.
        private void Place()
        {
            if (hero == null) return;
            float top = heroRenderer != null ? heroRenderer.bounds.max.y : hero.position.y + 2.3f;
            int heroX = Mathf.RoundToInt(hero.position.x * PixelsPerUnit);

            // The tail: whatever picture it has, its bottom middle sits right over the hero.
            Bounds tb = tail.sprite != null ? tail.sprite.bounds : new Bounds(new Vector3(2.5f, 1.5f) / PixelsPerUnit, new Vector3(5f, 3f) / PixelsPerUnit);
            int tailW = Mathf.Max(1, Mathf.RoundToInt(tb.size.x * PixelsPerUnit));
            int tailH = Mathf.Max(0, Mathf.RoundToInt(tb.size.y * PixelsPerUnit));
            int tailBottom = Mathf.RoundToInt(top * PixelsPerUnit) + gapAboveHead;
            int tailLeft = heroX - tailW / 2;
            tail.transform.position = new Vector3((tailLeft + tailW / 2f) / PixelsPerUnit - tb.center.x,
                                                  tailBottom / PixelsPerUnit - tb.min.y, 0f);

            int w = Mathf.Max(minWidth, Mathf.CeilToInt(lineBounds.size.x * PixelsPerUnit) + platePadding.x * 2);
            int h = Mathf.CeilToInt(lineBounds.size.y * PixelsPerUnit) + platePadding.y * 2;
            int left = heroX - w / 2;
            int bottom = tailBottom + tailH;
            plate.size = new Vector2(w, h) / PixelsPerUnit;
            plate.transform.position = new Vector3((left + w / 2f) / PixelsPerUnit, (bottom + h / 2f) / PixelsPerUnit, 0f);

            // The middle of the whole text sits in the middle of the plate.
            Vector2 plateCentre = new Vector2(left + w / 2f, bottom + h / 2f) / PixelsPerUnit;
            text.transform.position = new Vector3(plateCentre.x - lineBounds.center.x, plateCentre.y - lineBounds.center.y, 0f);
        }

        private void Advance()
        {
            int length = lines[lineIndex].Length;
            if (text.maxVisibleCharacters < length)
            {
                shownLetters = length;          // first press: the whole line at once
                text.maxVisibleCharacters = length;
                return;
            }
            lineIndex++;
            if (lineIndex < lines.Length) ShowLine();
            else End();
        }

        private static bool WalkPressed()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                                  kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame);
        }

        private static bool Pressed()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
