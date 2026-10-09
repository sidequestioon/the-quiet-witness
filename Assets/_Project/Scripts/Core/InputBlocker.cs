using UnityEngine;

namespace QuietWitness.Core
{
    // While something blocks input (travel, map, dialogue),
    // the investigator can't walk or interact.
    public static class InputBlocker
    {
        private static int count;

        public static bool IsBlocked => count > 0;
        public static void Push() => count++;
        public static void Pop() => count = Mathf.Max(0, count - 1);

        // Reset when Play mode starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => count = 0;
    }
}
