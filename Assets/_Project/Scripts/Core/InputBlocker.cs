using UnityEngine;

namespace QuietWitness.Core
{
    // While something blocks input (travel, map, dialogue),
    // the investigator can't walk or interact.
    public static class InputBlocker
    {
        private static int count;
        private static int releasedFrame = -1;

        // Stays blocked for the rest of the frame it was released in,
        // so the key that closed a dialogue doesn't open it again.
        public static bool IsBlocked => count > 0 || Time.frameCount == releasedFrame;

        public static void Push() => count++;

        public static void Pop()
        {
            count = Mathf.Max(0, count - 1);
            if (count == 0) releasedFrame = Time.frameCount;
        }

        // Reset when Play mode starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            count = 0;
            releasedFrame = -1;
        }
    }
}
