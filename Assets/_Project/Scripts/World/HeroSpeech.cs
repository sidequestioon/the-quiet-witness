using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.World
{
    // The hero's thoughts said out loud: a line of text on a black plate over his head,
    // typed out letter by letter.
    // All lines of a hotspot come together on one plate (or one per press: oneLineAtATime).
    // E / Space / Enter / click: show the whole text at once, then close.
    // He stands still while talking; A/D or the arrows close it and he walks off. Lines come from Hotspot Actions (the "Lines" of an action).
    public class HeroSpeech : MonoBehaviour
    {
        [Header("Size (can be tuned in Play mode and seen at once)")]
        [SerializeField] private float fontSize = 1.2f;
        [Tooltip("The plate never gets wider than this (pixels): longer text wraps onto more lines.")]
        [SerializeField] private int maxWidth = 130;
        [Tooltip("The plate is never narrower than this (pixels), even for a short line.")]
        [SerializeField] private int minWidth = 0;
        [Header("Look")]
        [SerializeField] private Color textColor = new Color(0.96f, 0.94f, 0.91f);
        [Tooltip("Pixels between the top of the hero's head and the plate.")]
        [SerializeField] private int gapAboveHead = 4;
        [SerializeField] private Color plateColor = new Color(0.055f, 0.05f, 0.063f, 1f);
        [Tooltip("Pixels of plate around the text: x = left/right, y = top/bottom.")]
        [SerializeField] private Vector2Int platePadding = new Vector2Int(4, 3);
        [SerializeField] private float lettersPerSecond = 45f;
        [Tooltip("Off: all lines of a hotspot are shown together on one plate. On: one line per E press.")]
        [SerializeField] private bool oneLineAtATime = false;
        [Tooltip("The hero is 10: the text is drawn over him.")]
        [SerializeField] private int sortingOrder = 30;
        [Tooltip("Unlit, so the plate stays black in any room light. Set by Setup.")]
        [SerializeField] private Material plateMaterial;

        private const float PixelsPerUnit = 36f;
        private const float FadeTime = 0.15f;

        private static HeroSpeech instance;

        private TextMeshPro text;
        private SpriteRenderer plate;
        private SpriteRenderer tail;
        private static Sprite tailSprite;
        private Bounds lineBounds;           // the whole current line, in the text's local space
        private float measuredSize;
        private int measuredWidth;
        private static Sprite plateSprite;
        private string[] lines;
        private int lineIndex;
        private float shownLetters;
        private bool speaking;
        private float fade;
        private int openedFrame;
        private Transform hero;
        private Renderer heroRenderer;

        public static bool IsSpeaking => instance != null && instance.speaking;

        // Shows the lines over the hero. False if there is no HeroSpeech in the scene.
        public static bool Say(string[] say) => Say(say, null);

        // A hotspot's lines, with its own plate width if it has one.
        public static bool Say(HotspotAction action) => action != null && Say(action.lines, action);

        private static bool Say(string[] say, HotspotAction source)
        {
            if (instance == null) instance = FindFirstObjectByType<HeroSpeech>();
            if (instance == null || say == null || say.Length == 0) return false;
            instance.source = source;
            instance.Begin(say);
            return true;
        }

        private HotspotAction source;   // where the lines came from (for its plate width)

        // This text's widest plate: the hotspot's own, or the usual one.
        private int MaxWidth => source != null && source.plateWidth > 0 ? source.plateWidth : maxWidth;

        private void Awake()
        {
            instance = this;
            var go = new GameObject("Speech");
            go.transform.SetParent(transform, false);
            text = go.AddComponent<TextMeshPro>();
            var prompt = GetComponent<InteractionPrompt>();
            if (prompt != null && prompt.Font != null) text.font = prompt.Font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Top;   // lines centred, typed from the top down
            text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.sortingOrder = sortingOrder;
            text.text = "";
            text.enabled = false;

            // Black plate with 1-pixel cut corners, stretched to the text (9-sliced).
            if (plateSprite == null)
            {
                var tex = new Texture2D(3, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                        tex.SetPixel(x, y, (x != 1 && y != 1) ? Color.clear : Color.white);
                tex.Apply();
                plateSprite = Sprite.Create(tex, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0,
                                            SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
            }
            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(transform, false);
            plate = plateGo.AddComponent<SpriteRenderer>();
            if (plateMaterial != null) plate.sharedMaterial = plateMaterial;
            plate.sprite = plateSprite;
            plate.drawMode = SpriteDrawMode.Sliced;
            plate.sortingOrder = sortingOrder - 1;
            plate.enabled = false;

            // Little triangle under the plate, pointing at the hero's head: he is the one talking.
            if (tailSprite == null)
            {
                var tex = new Texture2D(5, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 3; y++)          // y = 0 is the bottom row (the tip)
                    for (int x = 0; x < 5; x++)
                        tex.SetPixel(x, y, Mathf.Abs(x - 2) <= y ? Color.white : Color.clear);
                tex.Apply();
                tailSprite = Sprite.Create(tex, new Rect(0, 0, 5, 3), Vector2.zero, PixelsPerUnit);
            }
            var tailGo = new GameObject("Tail");
            tailGo.transform.SetParent(transform, false);
            tail = tailGo.AddComponent<SpriteRenderer>();
            if (plateMaterial != null) tail.sharedMaterial = plateMaterial;
            tail.sprite = tailSprite;
            tail.sortingOrder = sortingOrder - 1;
            tail.enabled = false;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Begin(string[] say)
        {
            if (!speaking) InputBlocker.Push();
            // "\\n" typed by hand also starts a new line (Enter in the Lines box does it too).
            var cleaned = new string[say.Length];
            for (int i = 0; i < say.Length; i++) cleaned[i] = say[i].Replace("\\n", "\n");
            lines = oneLineAtATime ? cleaned : new[] { string.Join(" ", cleaned) };
            lineIndex = 0;
            speaking = true;
            openedFrame = Time.frameCount;
            if (hero == null)
            {
                var mover = FindFirstObjectByType<PlayerMover>();
                if (mover != null)
                {
                    hero = mover.transform;
                    heroRenderer = mover.GetComponentInChildren<SpriteRenderer>();
                }
            }
            ShowLine();
        }

        private void ShowLine()
        {
            text.text = lines[lineIndex];
            shownLetters = 0f;
            // Switched on before measuring: a switched-off text keeps its old size, which made
            // the plate tiny the first time and the wrong size after that.
            text.enabled = true;
            plate.enabled = true;
            tail.enabled = true;
            Measure();
            Place();   // right away, so the first frame is already in the right spot
        }

        // Size of the whole line, so the plate is ready before the letters come.
        private void Measure()
        {
            text.fontSize = fontSize;
            text.rectTransform.sizeDelta = new Vector2(MaxWidth / PixelsPerUnit, 1f);
            text.maxVisibleCharacters = 99999;
            text.ForceMeshUpdate(true, true);
            lineBounds = text.textBounds;
            if (lineBounds.size.x <= 0f)   // font not ready yet: fall back to its preferred size
            {
                Vector2 size = text.GetPreferredValues(text.text, MaxWidth / PixelsPerUnit, 0f);
                Rect r = text.rectTransform.rect;   // the text hangs from the middle of the box's top
                lineBounds = new Bounds(new Vector3(r.center.x, r.yMax - size.y / 2f, 0f), new Vector3(size.x, size.y, 0f));
            }
            text.maxVisibleCharacters = Mathf.FloorToInt(shownLetters);
            measuredSize = fontSize;
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
            fade = Mathf.MoveTowards(fade, speaking ? 1f : 0f, Time.deltaTime / FadeTime);
            if (!speaking && fade <= 0f)
            {
                text.enabled = false;
                plate.enabled = false;
                tail.enabled = false;
                return;
            }

            var c = textColor; c.a *= fade; text.color = c;
            var pc = plateColor; pc.a *= fade; plate.color = pc; tail.color = pc;

            if (!Mathf.Approximately(measuredSize, fontSize) || measuredWidth != MaxWidth) Measure();

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

        private void LateUpdate()
        {
            if (speaking || fade > 0f) Place();
        }

        // Over the hero's head, following him; the plate on whole pixels.
        private void Place()
        {
            if (hero != null)
            {
                float top = heroRenderer != null ? heroRenderer.bounds.max.y : hero.position.y + 2.3f;
                int w = Mathf.Max(minWidth, Mathf.CeilToInt(lineBounds.size.x * PixelsPerUnit) + platePadding.x * 2);
                int h = Mathf.CeilToInt(lineBounds.size.y * PixelsPerUnit) + platePadding.y * 2;
                int left = Mathf.RoundToInt(hero.position.x * PixelsPerUnit) - w / 2;
                int bottom = Mathf.RoundToInt(top * PixelsPerUnit) + gapAboveHead + 3;   // 3 px for the tail
                int tipX = Mathf.RoundToInt(hero.position.x * PixelsPerUnit) - 2;
                tail.transform.position = new Vector3(tipX / PixelsPerUnit, (bottom - 3) / PixelsPerUnit, 0f);
                plate.size = new Vector2(w, h) / PixelsPerUnit;
                plate.transform.position = new Vector3((left + w / 2f) / PixelsPerUnit, (bottom + h / 2f) / PixelsPerUnit, 0f);

                // Put the text so the middle of the whole text sits in the middle of the plate.
                Vector2 plateCentre = new Vector2(left + w / 2f, bottom + h / 2f) / PixelsPerUnit;
                text.transform.position = new Vector3(plateCentre.x - lineBounds.center.x, plateCentre.y - lineBounds.center.y, 0f);
            }
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
