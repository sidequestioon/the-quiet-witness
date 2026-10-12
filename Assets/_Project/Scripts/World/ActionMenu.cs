using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.World
{
    // The small list that drops out of a hotspot's dot: a tiny icon and a word per action
    // (LOOK, TAKE, OPEN...). Drawn as pixel art at the game's own resolution.
    // Keys: W/S or arrows to move, E/Enter/Space to choose, Esc/Q to close.
    // Mouse: point at a line and click; click anywhere else to close.
    public class ActionMenu : MonoBehaviour
    {
        private const float PixelsPerUnit = 36f;
        private const float OpenTime = 0.12f;
        private const float CloseTime = 0.08f;
        private const int Row = 7;          // px per line
        private const int GlyphX = 3;       // where the little icon starts
        private const int TextX = 10;       // where the word starts
        private const int Top = 2;          // border + padding above the first line
        private const int Pointer = 3;      // little arrow under the panel, pointing at the dot

        private static readonly Color32 Fill = new Color32(0x2a, 0x27, 0x2e, 0xf2);
        private static readonly Color32 Border = new Color32(0x0e, 0x0d, 0x10, 0xff);
        private static readonly Color32 RowOn = new Color32(0x45, 0x36, 0x32, 0xff);
        private static readonly Color32 Marker = new Color32(0xd8, 0x46, 0x3a, 0xff);
        private static readonly Color32 Ink = new Color32(0xd8, 0xcc, 0xb0, 0xff);
        private static readonly Color32 InkOn = new Color32(0xff, 0xff, 0xff, 0xff);
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private static ActionMenu instance;
        public static bool IsOpen => instance != null && instance.open;

        private SpriteRenderer panel;
        private HotspotActions target;
        private int selected;
        private bool open;
        private float t;
        private int openedFrame;
        private int width, height;
        private Texture2D texture;

        public static void Open(HotspotActions actions, Vector2 dotTop, Material material)
        {
            if (actions == null || actions.Actions.Count == 0) return;
            if (instance == null)
            {
                var go = new GameObject("ActionMenu");
                instance = go.AddComponent<ActionMenu>();
                var child = new GameObject("Panel");
                child.transform.SetParent(go.transform, false);
                instance.panel = child.AddComponent<SpriteRenderer>();
                instance.panel.sortingOrder = 1000;
            }
            if (material != null) instance.panel.sharedMaterial = material;
            instance.Show(actions, dotTop);
        }

        private void Show(HotspotActions actions, Vector2 dotTop)
        {
            if (!open) InputBlocker.Push();
            target = actions;
            selected = 0;
            open = true;
            t = 0f;
            openedFrame = Time.frameCount;
            Render();

            // The arrow's tip sits 1 px above the dot, on the pixel grid.
            Vector2 tip = dotTop + new Vector2(0f, 1f / PixelsPerUnit);
            tip = new Vector2(Mathf.Round(tip.x * PixelsPerUnit), Mathf.Round(tip.y * PixelsPerUnit)) / PixelsPerUnit;
            transform.position = new Vector3(tip.x, tip.y, 0f);

            // Keep the panel on screen: slide it sideways if it would stick out.
            float leftPx = -(width / 2);
            var cam = Camera.main;
            if (cam != null)
            {
                float halfW = cam.orthographicSize * cam.aspect;
                float minX = cam.transform.position.x - halfW + 2f / PixelsPerUnit;
                float maxX = cam.transform.position.x + halfW - 2f / PixelsPerUnit;
                float x0 = tip.x + leftPx / PixelsPerUnit;
                float x1 = x0 + width / PixelsPerUnit;
                if (x0 < minX) leftPx += Mathf.Ceil((minX - x0) * PixelsPerUnit);
                else if (x1 > maxX) leftPx -= Mathf.Ceil((x1 - maxX) * PixelsPerUnit);
            }
            panel.transform.localPosition = new Vector3(leftPx / PixelsPerUnit, 0f, 0f);
            panel.enabled = true;
            Apply();
        }

        private void Close()
        {
            if (!open) return;
            open = false;
            InputBlocker.Pop();
        }

        private void Choose()
        {
            var chosen = target;
            int index = selected;
            Close();
            chosen.Choose(index);
        }

        private void Update()
        {
            if (open && Time.frameCount != openedFrame) HandleInput();

            t = Mathf.MoveTowards(t, open ? 1f : 0f, Time.deltaTime / (open ? OpenTime : CloseTime));
            if (!open && t <= 0f) panel.enabled = false;
            else Apply();
        }

        private void Apply()
        {
            float e = 1f - (1f - t) * (1f - t);
            transform.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, e), Mathf.Lerp(0.2f, 1f, e), 1f);
            var c = panel.color;
            c.a = Mathf.Clamp01(t * 1.5f);
            panel.color = c;
        }

        private void HandleInput()
        {
            int count = target.Actions.Count;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) { Close(); return; }
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Select((selected - 1 + count) % count);
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Select((selected + 1) % count);
                if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) { Choose(); return; }
            }

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return;
            Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Vector2 local = (world - (Vector2)panel.transform.position) * PixelsPerUnit;
            int px = Mathf.FloorToInt(local.x), pyFromTop = height - 1 - Mathf.FloorToInt(local.y);
            int row = (pyFromTop - Top) >= 0 ? (pyFromTop - Top) / Row : -1;
            bool overRow = px >= 1 && px < width - 1 && row >= 0 && row < count && pyFromTop < height - Pointer;
            if (overRow && mouse.delta.ReadValue().sqrMagnitude > 0f) Select(row);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (overRow) { Select(row); Choose(); }
                else Close();
            }
        }

        private void Select(int index)
        {
            if (index == selected) return;
            selected = index;
            Render();
        }

        // ---------- drawing ----------

        private void Render()
        {
            var actions = target.Actions;
            int textW = 0;
            foreach (var a in actions) textW = Mathf.Max(textW, PixelFont.Width(a.Label));
            width = TextX + textW + 3;
            int panelH = Top + actions.Count * Row + 2;
            height = panelH + Pointer;

            var px = new Color32[width * height];
            void Set(int x, int y, Color32 c) { if (x >= 0 && x < width && y >= 0 && y < height) px[(height - 1 - y) * width + x] = c; }

            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) Set(x, y, Clear);
            // panel with 1-px border and cut corners
            for (int y = 0; y < panelH; y++)
                for (int x = 0; x < width; x++)
                {
                    bool corner = (x == 0 || x == width - 1) && (y == 0 || y == panelH - 1);
                    if (corner) continue;
                    bool edge = x == 0 || y == 0 || x == width - 1 || y == panelH - 1;
                    Set(x, y, edge ? Border : Fill);
                }
            // pointer
            int c0 = width / 2;
            for (int i = 0; i < Pointer; i++)
            {
                int y = panelH - 1 + i, half = Pointer - 1 - i;
                for (int x = c0 - half; x <= c0 + half; x++) Set(x, y, Fill);
                Set(c0 - half, y, Border); Set(c0 + half, y, Border);
            }
            Set(c0, panelH - 1 + Pointer, Border);

            for (int i = 0; i < actions.Count; i++)
            {
                int top = Top + i * Row;
                bool on = i == selected;
                if (on)
                {
                    for (int y = top; y < top + Row; y++)
                        for (int x = 1; x < width - 1; x++) Set(x, y, RowOn);
                    for (int y = top; y < top + Row; y++) Set(1, y, Marker);
                }
                var ink = on ? InkOn : Ink;
                var glyph = Glyphs.For(actions[i].kind);
                int gy = top + 1 + (5 - glyph.Length) / 2;
                for (int r = 0; r < glyph.Length; r++)
                    for (int col = 0; col < glyph[r].Length; col++)
                        if (glyph[r][col] == '#') Set(GlyphX + col, gy + r, ink);
                PixelFont.Draw(actions[i].Label, TextX, top + 1, ink, Set);
            }

            if (texture == null || texture.width != width || texture.height != height)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            }
            texture.SetPixels32(px);
            texture.Apply();
            if (panel.sprite != null) Destroy(panel.sprite);
            panel.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero, PixelsPerUnit);
        }

        private static class Glyphs
        {
            private static readonly string[] Eye = { ".###.", "#.#.#", ".###." };
            private static readonly string[] Hand = { "#.#.#", "#.#.#", "#####", "#####", ".###." };
            private static readonly string[] Bubble = { ".###.", "#####", "#####", ".###.", ".#..." };
            private static readonly string[] Door = { "###..", "#..#.", "#.###", "#..#.", "###.." };
            private static readonly string[] Lock = { ".###.", ".#.#.", "#####", "##.##", "#####" };
            private static readonly string[] Key = { "##...", "#####", "##.#." };

            public static string[] For(ActionKind kind) => kind switch
            {
                ActionKind.Look => Eye,
                ActionKind.Take => Hand,
                ActionKind.Use => Hand,
                ActionKind.Open => Lock,
                ActionKind.Talk => Bubble,
                ActionKind.Leave => Door,
                ActionKind.UseItem => Key,
                _ => Eye
            };
        }
    }

    // A 3x5 capital-letter pixel font, drawn straight into pixels.
    public static class PixelFont
    {
        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { "010", "101", "111", "101", "101" }, ['B'] = new[] { "110", "101", "110", "101", "110" },
            ['C'] = new[] { "011", "100", "100", "100", "011" }, ['D'] = new[] { "110", "101", "101", "101", "110" },
            ['E'] = new[] { "111", "100", "110", "100", "111" }, ['F'] = new[] { "111", "100", "110", "100", "100" },
            ['G'] = new[] { "011", "100", "101", "101", "011" }, ['H'] = new[] { "101", "101", "111", "101", "101" },
            ['I'] = new[] { "111", "010", "010", "010", "111" }, ['J'] = new[] { "001", "001", "001", "101", "010" },
            ['K'] = new[] { "101", "101", "110", "101", "101" }, ['L'] = new[] { "100", "100", "100", "100", "111" },
            ['M'] = new[] { "101", "111", "111", "101", "101" }, ['N'] = new[] { "110", "101", "101", "101", "101" },
            ['O'] = new[] { "010", "101", "101", "101", "010" }, ['P'] = new[] { "110", "101", "110", "100", "100" },
            ['Q'] = new[] { "010", "101", "101", "110", "011" }, ['R'] = new[] { "110", "101", "110", "101", "101" },
            ['S'] = new[] { "011", "100", "010", "001", "110" }, ['T'] = new[] { "111", "010", "010", "010", "010" },
            ['U'] = new[] { "101", "101", "101", "101", "111" }, ['V'] = new[] { "101", "101", "101", "101", "010" },
            ['W'] = new[] { "101", "101", "111", "111", "101" }, ['X'] = new[] { "101", "101", "010", "101", "101" },
            ['Y'] = new[] { "101", "101", "010", "010", "010" }, ['Z'] = new[] { "111", "001", "010", "100", "111" },
            ['0'] = new[] { "111", "101", "101", "101", "111" }, ['1'] = new[] { "010", "110", "010", "010", "111" },
            ['2'] = new[] { "110", "001", "010", "100", "111" }, ['3'] = new[] { "110", "001", "010", "001", "110" },
            ['4'] = new[] { "101", "101", "111", "001", "001" }, ['5'] = new[] { "111", "100", "110", "001", "110" },
            ['6'] = new[] { "011", "100", "111", "101", "111" }, ['7'] = new[] { "111", "001", "010", "010", "010" },
            ['8'] = new[] { "111", "101", "111", "101", "111" }, ['9'] = new[] { "111", "101", "111", "001", "110" },
            ['.'] = new[] { "0", "0", "0", "0", "1" }, ['!'] = new[] { "1", "1", "1", "0", "1" },
            ['?'] = new[] { "110", "001", "010", "000", "010" }, ['-'] = new[] { "000", "000", "111", "000", "000" },
        };

        public static int Width(string text)
        {
            int w = 0;
            foreach (char ch in text) w += Advance(ch);
            return Mathf.Max(0, w - 1);
        }

        public static void Draw(string text, int x, int y, Color32 color, System.Action<int, int, Color32> set)
        {
            foreach (char raw in text)
            {
                char ch = char.ToUpperInvariant(raw);
                if (Glyphs.TryGetValue(ch, out var g))
                    for (int r = 0; r < g.Length; r++)
                        for (int c = 0; c < g[r].Length; c++)
                            if (g[r][c] == '1') set(x + c, y + r, color);
                x += Advance(ch);
            }
        }

        private static int Advance(char ch) => ch == ' ' ? 3 : Glyphs.TryGetValue(char.ToUpperInvariant(ch), out var g) ? g[0].Length + 1 : 4;
    }
}
