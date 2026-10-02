using UnityEngine;

namespace MVP04
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class KnockableCandleStand : MonoBehaviour
    {
        [SerializeField] private Light glowLight;
        [SerializeField] private Renderer flameRenderer;
        [SerializeField] private CandleSway sway;
        [Header("Knock over")]
        [SerializeField, Range(10, 80), Tooltip("Extinguish once the stand tilts this far from world up.")]
        private float extinguishAngle = 32;
        [SerializeField, Min(0), Tooltip("Horizontal impulse at normal walking speed, in newton seconds.")]
        private float pushImpulse = 5;
        [SerializeField, Min(.05f)] private float pushCooldown = .3f;

        private Rigidbody body;
        private float nextPushTime;
        public bool IsExtinguished { get; private set; }

        public void Configure(Light light, Renderer flames, CandleSway candleSway)
        {
            glowLight = light;
            flameRenderer = flames;
            sway = candleSway;
        }

        private void Awake() => body = GetComponent<Rigidbody>();

        private void OnEnable()
        {
            if (IsExtinguished) StopFlame();
        }

        public void TryPush(Vector3 direction, Vector3 contactPoint, float walkingSpeed)
        {
            if (!isActiveAndEnabled || body == null || body.isKinematic || Time.time < nextPushTime) return;
            direction.y = 0;
            if (direction.sqrMagnitude < .001f || walkingSpeed < .1f) return;
            nextPushTime = Time.time + pushCooldown;
            // CharacterController is not a dynamic body: transfer its bump explicitly.
            // An upright stand is pushed at torso height; a fallen one is pushed at contact.
            if (Vector3.Dot(transform.up, Vector3.up) > .85f)
                contactPoint.y = Mathf.Clamp(contactPoint.y, transform.position.y + .95f, transform.position.y + 1.3f);
            body.AddForceAtPosition(direction.normalized * (pushImpulse * Mathf.Clamp01(walkingSpeed / 3.4f)),
                contactPoint, ForceMode.Impulse);
        }

        private void FixedUpdate()
        {
            if (IsExtinguished || body.IsSleeping()) return;
            if (Vector3.Dot(transform.up, Vector3.up) > Mathf.Cos(extinguishAngle * Mathf.Deg2Rad)) return;
            IsExtinguished = true;
            StopFlame();
        }

        private void StopFlame()
        {
            // Sway restores its captured state on disable, so stop it before hiding light/emission.
            if (sway != null) sway.enabled = false;
            if (glowLight != null) glowLight.enabled = false;
            if (flameRenderer != null) flameRenderer.enabled = false;
        }
    }
}
