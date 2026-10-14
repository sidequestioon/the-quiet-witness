using QuietWitness.Map;
using UnityEngine;

namespace QuietWitness.Cutscenes
{
    // Scene-side link to the close-ups and the map that live on Systems:
    // pick one of these in Hotspot Steps > Then (or Hotspot Actions > On Chosen).
    public class CloseUpOpener : MonoBehaviour
    {
        public void OpenCodeLock()
        {
            if (CodeLockView.Instance != null) CodeLockView.Instance.Open();
            else Debug.LogWarning("[CloseUp] No code lock: run Setup > Prologue story.");
        }

        public void OpenEnlarger()
        {
            if (DevelopView.Instance != null) DevelopView.Instance.Open();
            else Debug.LogWarning("[CloseUp] No enlarger close-up: run Setup > Prologue story.");
        }

        public void OpenMap()
        {
            if (MapPanel.Instance != null) MapPanel.Instance.Open();
        }
    }
}
