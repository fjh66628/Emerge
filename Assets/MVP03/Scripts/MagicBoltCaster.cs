using UnityEngine;
using UnityEngine.InputSystem;

namespace MVP03
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(PixelPilgrim))]
    public sealed class MagicBoltCaster : MonoBehaviour
    {
        [SerializeField] private MagicBolt projectilePrefab;
        [SerializeField] private MagicCastSigil sigilPrefab;
        [SerializeField, Min(.05f)] private float chargeDuration = .6f;
        [SerializeField, Min(.1f)] private float cooldown = .35f;
        [SerializeField] private float castHeight = 1.05f;
        private PixelPilgrim pilgrim;
        private float nextCastTime;
        private bool heldCast;
        private MagicCastSigil pendingSigil;
        private Vector3 pendingDirection;
        public bool IsCharging => pendingSigil != null;

        public void Configure(MagicBolt prefab) { projectilePrefab = prefab; sigilPrefab = null; }
        public void Configure(MagicBolt prefab, MagicCastSigil sigil, float charge, float recovery, float height = 1.05f)
        { projectilePrefab = prefab; sigilPrefab = sigil; chargeDuration = charge; cooldown = recovery; castHeight = height; }

        private void Awake() => pilgrim = GetComponent<PixelPilgrim>();

        private void Update()
        {
            if (pendingSigil != null && pendingSigil.IsReady)
            {
                Fire(pendingSigil.CastOrigin, pendingSigil.LaunchPosition, pendingDirection);
                pendingSigil.Release();
                pendingSigil = null;
            }
            Keyboard keys = PlayerKeyboard.Current;
            bool pressed = keys != null && keys.spaceKey.isPressed;
            if (pressed && !heldCast) TryCast();
            heldCast = pressed;
        }

        public bool TryCast()
        {
            if (!Application.isPlaying || projectilePrefab == null || IsCharging || Time.time < nextCastTime) return false;
            if (pilgrim == null) pilgrim = GetComponent<PixelPilgrim>();
            Vector3 direction = pilgrim.FacingDirection;
            if (sigilPrefab != null)
            {
                pendingDirection = direction;
                pendingSigil = Instantiate(sigilPrefab);
                pendingSigil.Begin(transform, direction, castHeight, chargeDuration);
                nextCastTime = Time.time + chargeDuration + cooldown;
            }
            else
            {
                Vector3 origin = transform.position + Vector3.up * castHeight;
                Fire(origin, origin, direction);
                nextCastTime = Time.time + cooldown;
            }
            return true;
        }

        private void Fire(Vector3 origin, Vector3 position, Vector3 direction)
        {
            var bolt = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction, Vector3.up));
            bolt.Launch(transform, direction, origin);
        }

        private void OnDisable()
        {
            if (pendingSigil != null) Destroy(pendingSigil.gameObject);
            pendingSigil = null; heldCast = false;
        }
    }
}
