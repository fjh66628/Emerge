using UnityEngine;

namespace MVP04
{
    // Associates a real shadow-casting spotlight with the aperture used by the volume pass.
    [DisallowMultipleComponent, RequireComponent(typeof(Light))]
    public sealed class SideWindowLight : MonoBehaviour
    {
        public Vector3 windowCentre;
        [Range(0, 2)] public float scattering = .85f;
    }
}
