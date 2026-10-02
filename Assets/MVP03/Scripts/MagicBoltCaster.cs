using UnityEngine;
using UnityEngine.InputSystem;

namespace MVP03
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(PixelPilgrim))]
    public sealed class MagicBoltCaster : MonoBehaviour
    {
        [SerializeField] private MagicBolt projectilePrefab;
        [SerializeField, Min(.1f)] private float cooldown = .35f;
        [SerializeField] private float castHeight = 1.05f;
        private PixelPilgrim pilgrim;
        private float nextCastTime;
        private bool heldCast;

        public void Configure(MagicBolt prefab) => projectilePrefab = prefab;

        private void Awake() => pilgrim = GetComponent<PixelPilgrim>();

        private void Update()
        {
            bool pressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            if (pressed && !heldCast) TryCast();
            heldCast = pressed;
        }

        public bool TryCast()
        {
            if (!Application.isPlaying || projectilePrefab == null || Time.time < nextCastTime) return false;
            if (pilgrim == null) pilgrim = GetComponent<PixelPilgrim>();
            Vector3 direction = pilgrim.FacingDirection;
            // Start at the torso: the first sweep also checks the path out of the caster.
            var bolt = Instantiate(projectilePrefab, transform.position + Vector3.up * castHeight,
                Quaternion.LookRotation(direction, Vector3.up));
            bolt.Launch(transform, direction);
            nextCastTime = Time.time + cooldown;
            return true;
        }
    }
}
