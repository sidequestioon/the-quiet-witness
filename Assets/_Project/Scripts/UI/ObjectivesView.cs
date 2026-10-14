using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // The Objectives page of the case file:
    //   (o) CURRENT OBJECTIVE
    //    :  Find out who sent the envelope, and why.      <- main goal
    //    :-- Be at the Riviera for the last show.          <- step
    //    :     . Find row seven, seat twelve.              <- mini step
    //    '-- ~~Take a closer look at the film frames.~~    <- done = crossed out
    // A dashed line runs down the left from the circle to the last step.
    // Looks: the hidden templates under Templates (Main, Step, Mini) - every line is a copy of one.
    // Preview: shows the Objectives list right in the editor (goals in Preview Done are crossed out).
    [ExecuteAlways]
    public class ObjectivesView : MonoBehaviour
    {
        [SerializeField] private ObjectiveList objectives;
        [Tooltip("The lines go here, one under another, from its top-left corner.")]
        [SerializeField] private RectTransform list;

        [Header("Templates (hidden): every line copies one of these")]
        [SerializeField] private ObjectiveRow mainTemplate;
        [SerializeField] private ObjectiveRow stepTemplate;
        [SerializeField] private ObjectiveRow miniTemplate;

        [Header("Dashed line")]
        [Tooltip("Runs down from under the main goal's circle to the last step. Colour and width on the picture itself.")]
        [SerializeField] private Image dashes;
        [Tooltip("Gap between the circle and the top of the dashed line.")]
        [SerializeField] private float dashGap = 6f;

        [SerializeField] private string noGoals = "Nothing to do. For now.";

        [Header("Preview outside Play mode")]
        [SerializeField] private bool preview = true;
        [Tooltip("Ids of goals shown crossed out in the preview.")]
        [SerializeField] private List<string> previewDone = new List<string> { "film" };

        private struct Entry
        {
            public ObjectiveRow template;
            public string text;
            public bool done;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<ObjectiveRow> rows = new List<ObjectiveRow>();
        private readonly List<ObjectiveRow> rowTemplates = new List<ObjectiveRow>();
        private bool cleaned;

        // Called by the case file when the page opens.
        public void Show(ObjectiveList.Goal main, List<ObjectiveList.Goal> steps)
        {
            entries.Clear();
            if (main == null && (steps == null || steps.Count == 0))
                entries.Add(new Entry { template = mainTemplate, text = noGoals });
            if (main != null)
                entries.Add(new Entry { template = mainTemplate, text = main.text, done = ObjectiveList.Done(main) });
            bool stepDone = false;
            if (steps != null)
                foreach (var s in steps)
                {
                    bool done = ObjectiveList.Done(s) || (s.mini && stepDone);   // a done step crosses out its mini steps
                    if (!s.mini) stepDone = done;
                    entries.Add(new Entry { template = s.mini ? miniTemplate : stepTemplate, text = s.text, done = done });
                }
            Layout();
        }

        private void Update()
        {
            if (Application.isPlaying) return;
            entries.Clear();
            if (preview && objectives != null)
            {
                // The first main goal and the steps under it (or, with no main goal, the steps from the start).
                int start = objectives.goals.FindIndex(g => g.main);
                if (start >= 0)
                {
                    var m = objectives.goals[start];
                    entries.Add(new Entry { template = mainTemplate, text = m.text, done = previewDone.Contains(m.id) });
                }
                bool stepDone = false;
                for (int i = start + 1; i < objectives.goals.Count && !objectives.goals[i].main; i++)
                {
                    var g = objectives.goals[i];
                    bool done = previewDone.Contains(g.id) || (g.mini && stepDone);
                    if (!g.mini) stepDone = done;
                    entries.Add(new Entry { template = g.mini ? miniTemplate : stepTemplate, text = g.text, done = done });
                }
            }
            Layout();
        }

        private void Layout()
        {
            if (list == null) return;
            CleanOnce();

            float y = 0f;
            ObjectiveRow mainRow = null, last = null;
            int used = 0;
            foreach (var e in entries)
            {
                if (e.template == null || e.template.text == null) continue;
                var row = RowAt(used++, e.template);

                // Text and colours
                string text = e.done ? $"<s>{e.text}</s>" : e.text;
                if (row.text.text != text) row.text.text = text;
                var color = e.done ? e.template.doneColor : e.template.text.color;
                if (row.text.color != color) row.text.color = color;
                if (row.marker != null && e.template.marker != null)
                {
                    var mc = e.done ? e.template.doneMarker : e.template.marker.color;
                    if (row.marker.color != mc) row.marker.color = mc;
                }

                // Height from the text, then one under another
                var tr = row.text.rectTransform;
                float top = -tr.anchoredPosition.y;
                float h = Mathf.Ceil(row.text.GetPreferredValues(text, tr.rect.width, 0f).y);
                if (!Mathf.Approximately(tr.sizeDelta.y, h)) tr.sizeDelta = new Vector2(tr.sizeDelta.x, h);
                var r = (RectTransform)row.transform;
                var pos = new Vector2(r.anchoredPosition.x, -y);
                if (r.anchoredPosition != pos) r.anchoredPosition = pos;
                var size = new Vector2(r.sizeDelta.x, top + h);
                if (r.sizeDelta != size) r.sizeDelta = size;
                y += top + h + e.template.spaceAfter;

                if (e.template == mainTemplate && mainRow == null) mainRow = row;
                last = row;
            }
            for (int i = used; i < rows.Count; i++)
                if (rows[i] != null && rows[i].gameObject.activeSelf) rows[i].gameObject.SetActive(false);

            PlaceDashes(mainRow, last);
        }

        private void PlaceDashes(ObjectiveRow mainRow, ObjectiveRow last)
        {
            if (dashes == null) return;
            bool on = mainRow != null && mainRow.marker != null && last != null && last != mainRow;
            if (dashes.gameObject.activeSelf != on) dashes.gameObject.SetActive(on);
            if (!on) return;

            // In the list's own units, from its top-left corner.
            var corner = new Vector2(list.rect.xMin, list.rect.yMax);
            var circle = Centre(mainRow.marker.rectTransform) - corner;
            float half = mainRow.marker.rectTransform.rect.height / 2f;
            float bottom = (last.marker != null ? Centre(last.marker.rectTransform) : Centre(last.text.rectTransform)) .y - corner.y;
            float topY = circle.y - half - dashGap;
            var d = dashes.rectTransform;
            var pos = new Vector2(circle.x, topY);
            if (d.anchoredPosition != pos) d.anchoredPosition = pos;
            var size = new Vector2(d.sizeDelta.x, Mathf.Max(0f, topY - bottom));
            if (d.sizeDelta != size) d.sizeDelta = size;
        }

        private Vector2 Centre(RectTransform r) => list.InverseTransformPoint(r.TransformPoint(r.rect.center));

        private ObjectiveRow RowAt(int i, ObjectiveRow template)
        {
            while (rows.Count <= i) { rows.Add(null); rowTemplates.Add(null); }
            var row = rows[i];
            if (row == null || rowTemplates[i] != template)
            {
                if (row != null) Kill(row.gameObject);
                row = Instantiate(template, list, false);
                row.name = template.name;
                if (!Application.isPlaying) row.gameObject.hideFlags = HideFlags.DontSave;   // preview copies are never saved
                rows[i] = row;
                rowTemplates[i] = template;
            }
            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
            return row;
        }

        // Copies left over from before (a script reload, the editor preview) are removed once.
        private void CleanOnce()
        {
            if (cleaned) return;
            cleaned = true;
            rows.Clear();
            rowTemplates.Clear();
            var old = new List<GameObject>();
            foreach (Transform child in list)
                if (child.GetComponent<ObjectiveRow>() != null) old.Add(child.gameObject);
            foreach (var go in old) Kill(go);
        }

        private static void Kill(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }
}
