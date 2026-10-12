using System.Collections.Generic;
using System.Linq;
using QuietWitness.World;
using UnityEditor;
using UnityEngine;

namespace QuietWitness.EditorTools
{
    // Hotspot Actions > Line and Prologue Intro > Get Up Line: a list of the lines in Hero Lines,
    // with the chosen line's text and plate width right under it. Editing them here changes
    // the line in Resources/HeroLines.asset. "+ New line" adds a line for this object.
    [CustomPropertyDrawer(typeof(HeroLineAttribute))]
    public class HeroLineDrawer : PropertyDrawer
    {
        private const string NoneOption = "(none: use Lines below)";
        private const string NewOption = "+ New line";
        private const float Gap = 2f;
        private static float Row => EditorGUIUtility.singleLineHeight;

        private static GUIStyle wrapStyle;
        private static GUIStyle Wrap => wrapStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };

        // The width the text box had last time, so its height fits the text.
        private static readonly Dictionary<string, float> widths = new Dictionary<string, float>();

        private static HeroLines Asset() => AssetDatabase.LoadAssetAtPath<HeroLines>(HeroLinesSetup.AssetPath);

        private static int IndexOf(HeroLines asset, string id) =>
            asset == null || string.IsNullOrWhiteSpace(id) ? -1 : asset.lines.FindIndex(l => l != null && l.id == id.Trim());

        private static string Key(SerializedProperty p) => p.serializedObject.targetObject.GetInstanceID() + "/" + p.propertyPath;

        private static float TextHeight(string text, float width) =>
            Mathf.Max(Wrap.CalcHeight(new GUIContent(text ?? string.Empty), Mathf.Max(60f, width)), Row * 2.5f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String) return Row;
            var asset = Asset();
            int i = IndexOf(asset, property.stringValue);
            if (i < 0) return string.IsNullOrWhiteSpace(property.stringValue) || asset == null ? Row : Row + Gap + Row * 2f;
            if (!widths.TryGetValue(Key(property), out float width))
                width = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 50f;
            return Row + Gap + TextHeight(asset.lines[i].text, width) + Gap + Row;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            EditorGUI.BeginProperty(position, label, property);
            var row = new Rect(position.x, position.y, position.width, Row);
            var field = EditorGUI.PrefixLabel(row, label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var asset = Asset();
            if (asset == null)
            {
                property.stringValue = EditorGUI.TextField(field, property.stringValue);
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }

            // The list: none, every line by id, new.
            var ids = asset.lines.Select(l => l != null ? l.id ?? string.Empty : string.Empty).ToList();
            var options = new List<string> { NoneOption };
            options.AddRange(ids.Select(id => id.Replace('/', '∕')));   // a "/" would make a submenu
            options.Add(NewOption);
            string current = property.stringValue != null ? property.stringValue.Trim() : string.Empty;
            int index = ids.IndexOf(current);
            int shown = index >= 0 ? index + 1 : 0;
            int pick = EditorGUI.Popup(field, shown, options.ToArray());
            if (pick != shown)
            {
                if (pick == 0) property.stringValue = string.Empty;
                else if (pick == options.Count - 1) property.stringValue = AddLine(asset, property);
                else property.stringValue = ids[pick - 1];
                current = property.stringValue;
                index = IndexOf(asset, current);
            }

            float y = row.yMax + Gap;
            if (index >= 0)
            {
                widths[Key(property)] = field.width;
                var so = new SerializedObject(asset);
                var element = so.FindProperty("lines").GetArrayElementAtIndex(index);
                var text = element.FindPropertyRelative("text");
                var plateWidth = element.FindPropertyRelative("plateWidth");

                float h = TextHeight(text.stringValue, field.width);
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUI.TextArea(new Rect(field.x, y, field.width, h), text.stringValue, Wrap);
                if (EditorGUI.EndChangeCheck()) text.stringValue = typed;
                y += h + Gap;

                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80f;
                plateWidth.intValue = EditorGUI.IntField(new Rect(field.x, y, field.width, Row),
                    new GUIContent("Plate width", "Widest the plate may get for this line, in pixels. 0 = Hero Speech > Max Width."),
                    plateWidth.intValue);
                EditorGUIUtility.labelWidth = labelWidth;
                so.ApplyModifiedProperties();
            }
            else if (current.Length > 0)
            {
                EditorGUI.HelpBox(new Rect(field.x, y, field.width, Row * 2f),
                    $"There is no line \"{current}\" in Hero Lines: the Lines below are used.", MessageType.Warning);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        // A new empty line named after this object, e.g. office.look_aquarium.
        private static string AddLine(HeroLines asset, SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            string scene = target is Component c && c.gameObject.scene.IsValid() ? c.gameObject.scene.name : "game";
            string baseId = HeroLinesSetup.IdFor(scene, target.name, 0);
            string id = baseId;
            for (int n = 2; asset.lines.Exists(l => l != null && l.id == id); n++) id = baseId + "." + n;
            Undo.RecordObject(asset, "New hero line");
            asset.lines.Add(new HeroLines.Line { id = id, text = string.Empty });
            EditorUtility.SetDirty(asset);
            return id;
        }
    }
}
