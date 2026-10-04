using UnityEngine;

namespace MVP03
{
    [CreateAssetMenu(menuName = "MVP03/Courtyard sunlight settings")]
    public sealed class CourtyardSunSettings : ScriptableObject
    {
        public bool sourceEnabled = true;
        public Vector3 rotation = new Vector3(39.82728f, 166.43457f, 0);
        public Color colour = new Color(1, .9f, .72f);
        [Min(0)] public float intensity = 3.4f;
        [Range(0, 1)] public float shadowStrength = 1;
    }
}
