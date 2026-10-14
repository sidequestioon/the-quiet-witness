using QuietWitness.Cutscenes;
using QuietWitness.Map;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The map's info plate (Systems > TownMap > ... > Info) gets the look of the envelope plate:
    // the same dark plate picture and colour, the little tail pointing at the dot, the text centred
    // in the same font size and colour. It sits over the dot (or under it), not to the side.
    // At the bottom, after a faint line: the round walking-man icon and "Go there" (when he can go).
    // Safe to run again. Menu: Tools > The Quiet Witness > Setup > Map: info plate like the envelope
    public static class MapInfoStyleSetup
    {
        [MenuItem("Tools/The Quiet Witness/Setup/Map: info plate like the envelope")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Map", "Stop Play mode first.", "OK");
                return;
            }

            // The envelope plate, from the office.
            if (!OpenScene(Scene("Office"))) return;
            var inspect = Object.FindFirstObjectByType<EnvelopeInspect>(FindObjectsInactive.Include);
            if (inspect == null)
            {
                EditorUtility.DisplayDialog("Map", "No envelope close-up in the office: run Setup > Envelope inspect first.", "OK");
                return;
            }
            var eso = new SerializedObject(inspect);
            var plate = eso.FindProperty("captionPlate").objectReferenceValue as Image;
            var tail = eso.FindProperty("captionTail").objectReferenceValue as Image;
            var text = eso.FindProperty("captionText").objectReferenceValue as TMP_Text;
            var paddingProp = eso.FindProperty("platePadding");
            Vector2 padding = paddingProp != null ? paddingProp.vector2Value : new Vector2(24f, 16f);
            if (plate == null)
            {
                EditorUtility.DisplayDialog("Map", "The envelope close-up has no plate (Caption).", "OK");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var map = root.GetComponentInChildren<TownMap>(true);
                var mso = map != null ? new SerializedObject(map) : null;
                var info = mso?.FindProperty("info").objectReferenceValue as RectTransform;
                if (info == null)
                {
                    EditorUtility.DisplayDialog("Map", "No town map on Systems: run Setup > Town map first.", "OK");
                    return;
                }

                // The plate
                var image = info.GetComponent<Image>();
                image.sprite = plate.sprite;
                image.type = plate.type;
                image.pixelsPerUnitMultiplier = plate.pixelsPerUnitMultiplier;
                image.color = plate.color;
                info.pivot = new Vector2(0.5f, 0.5f);
                if (info.TryGetComponent<VerticalLayoutGroup>(out var layout))
                {
                    layout.padding = new RectOffset(Mathf.RoundToInt(padding.x), Mathf.RoundToInt(padding.x),
                                                    Mathf.RoundToInt(padding.y), Mathf.RoundToInt(padding.y));
                    layout.childAlignment = TextAnchor.UpperCenter;
                }

                // The texts: centred; the description like the envelope words
                foreach (var t in info.GetComponentsInChildren<TMP_Text>(true))
                    t.alignment = TextAlignmentOptions.Top;
                var infoText = mso.FindProperty("infoText").objectReferenceValue as TMP_Text;
                if (infoText != null && text != null)
                {
                    infoText.font = text.font;
                    infoText.fontSharedMaterial = text.font != null ? text.font.material : text.fontSharedMaterial;
                    infoText.fontSize = text.fontSize;
                    infoText.color = text.color;
                    infoText.fontStyle = FontStyles.Normal;
                }

                // The tail pointing at the dot
                var old = info.Find("Tail");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                Image infoTail = null;
                if (tail != null)
                {
                    var go = new GameObject("Tail", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    go.transform.SetParent(info, false);
                    go.GetComponent<LayoutElement>().ignoreLayout = true;
                    infoTail = go.GetComponent<Image>();
                    infoTail.sprite = tail.sprite;
                    infoTail.type = tail.type;
                    infoTail.pixelsPerUnitMultiplier = tail.pixelsPerUnitMultiplier;
                    infoTail.color = plate.color;
                    infoTail.raycastTarget = false;
                    var r = infoTail.rectTransform;
                    r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = tail.rectTransform.sizeDelta;
                }
                mso.FindProperty("infoTail").objectReferenceValue = infoTail;

                // A faint line, then the round icon and "Go there" in one row.
                foreach (var name in new[] { "Divider", "ActionRow" })
                {
                    var o = info.Find(name);
                    if (o == null) continue;
                    var keep = o.GetComponentsInChildren<TMP_Text>(true);   // the Action text lives in the row
                    foreach (var k in keep) k.transform.SetParent(info, false);
                    Object.DestroyImmediate(o.gameObject);
                }
                var action = mso.FindProperty("infoAction").objectReferenceValue as TMP_Text;
                var yellow = action != null ? action.color : new Color(0.94f, 0.85f, 0.64f, 1f);

                var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                divider.transform.SetParent(info, false);
                var line = divider.GetComponent<Image>();
                line.color = new Color(yellow.r, yellow.g, yellow.b, 0.18f);
                line.raycastTarget = false;
                var le = divider.GetComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 2f;

                var row = new GameObject("ActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(info, false);
                var h = row.GetComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleCenter;
                h.spacing = 10f;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;

                var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectRoot + "/Art/UI/Map/Go_Icon.png");
                var iconGo = new GameObject("GoIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconGo.transform.SetParent(row.transform, false);
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = iconSprite;
                icon.raycastTarget = false;
                var ile = iconGo.GetComponent<LayoutElement>();
                float side = iconSprite != null ? iconSprite.rect.width * 3f : 39f;   // 3x, like the board
                ile.minWidth = ile.preferredWidth = side;
                ile.minHeight = ile.preferredHeight = side;

                if (action != null)
                {
                    action.transform.SetParent(row.transform, false);
                    action.alignment = TextAlignmentOptions.Left;
                    action.textWrappingMode = TextWrappingModes.NoWrap;
                }
                // keep the order: Title, Text, Divider, ActionRow, Tail
                divider.transform.SetSiblingIndex(info.childCount - 1);
                row.transform.SetSiblingIndex(info.childCount - 1);
                if (infoTail != null) infoTail.transform.SetAsLastSibling();

                mso.FindProperty("infoDivider").objectReferenceValue = divider;
                mso.FindProperty("infoActionRow").objectReferenceValue = row;
                mso.FindProperty("infoGoIcon").objectReferenceValue = iconGo;
                mso.FindProperty("clickAgainText").stringValue = "Go there";
                mso.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] The map's info plate now looks like the envelope plate. To see it: Systems > TownMap > Preview.");
        }
    }
}
