using UnityEngine;

namespace QuietWitness.World
{
    // A mood of light: morning, day, evening or night.
    [CreateAssetMenu(fileName = "LightingPreset", menuName = "The Quiet Witness/Lighting Preset")]
    public class LightingPreset : ScriptableObject
    {
        [SerializeField] private Color globalColor = Color.white;
        [Range(0f, 1.5f)]
        [SerializeField] private float globalIntensity = 1f;
        [Tooltip("Desk lamps, street lights and neon are on.")]
        [SerializeField] private bool lampsOn;

        public Color GlobalColor => globalColor;
        public float GlobalIntensity => globalIntensity;
        public bool LampsOn => lampsOn;
    }
}