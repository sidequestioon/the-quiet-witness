using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.UI
{
    // "Call it a day": put it on an Interactable (the office door, the coat rack).
    // If the key clues of this time segment are found, asks to move on and shows
    // the segment's warning (what will close forever). Otherwise says "not yet".
    public class TimeAdvancer : MonoBehaviour
    {
        [TextArea(2, 4)]
        [SerializeField] private string notReadyText = "Not yet. There's still work to do.";

        public void Use()
        {
            var time = TimeManager.Instance;
            var confirm = ConfirmPanel.Instance;
            if (time == null || confirm == null) return;

            if (!time.IsReady)
            {
                confirm.Ask("Not yet", notReadyText, "OK", null, null);
                return;
            }

            string warning = string.IsNullOrWhiteSpace(time.Current.Warning)
                ? "Time will move on."
                : time.Current.Warning;
            confirm.Ask($"End {time.Current.DisplayName}?", warning, "Move on", "Not yet", time.Advance);
        }
    }
}
