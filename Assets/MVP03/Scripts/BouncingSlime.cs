using UnityEngine;

namespace MVP03
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class BouncingSlime : MonoBehaviour, IMagicHitReceiver
    {
        [SerializeField] private SpriteRenderer portrait;
        [SerializeField] private Transform target;
        [SerializeField] private Camera view;
        [SerializeField, Min(0)] private float detectionRange = 8.5f;
        [SerializeField, Min(.1f)] private float hopHeight = .55f;
        [SerializeField, Min(0)] private float hopSpeed = 2.4f;
        [SerializeField, Min(.2f)] private float restDuration = .7f;
        [SerializeField, Min(.1f)] private float gravity = 14f;
        [SerializeField, Min(.1f)] private float stopDistance = 1.15f;
        [SerializeField, Min(1)] private int hitPoints = 2;
        [SerializeField, Min(1)] private float respawnDelay = 4f;

        private CharacterController controller;
        private MaterialPropertyBlock properties;
        private Vector3 home, restScale, hopVelocity, recoil;
        private float verticalSpeed, waitTimer, landingTimer, flashTimer, stunTimer, deathTimer;
        private bool airborne, defeated;
        public int RemainingHits { get; private set; }
        public int HopCount { get; private set; }
        public bool IsAirborne => airborne;
        public bool IsDefeated => defeated;

        public void Configure(SpriteRenderer sprite, Transform player, Camera camera)
        { portrait = sprite; target = player; view = camera; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (portrait == null) portrait = GetComponentInChildren<SpriteRenderer>();
            restScale = portrait.transform.localScale;
            home = transform.position;
            properties = new MaterialPropertyBlock();
            RemainingHits = hitPoints;
            waitTimer = .45f;
        }

        private void Start()
        {
            if (target == null) target = FindFirstObjectByType<PixelPilgrim>()?.transform;
            if (view == null) view = Camera.main;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (defeated)
            {
                deathTimer += dt;
                float t = Mathf.Clamp01(deathTimer / .28f);
                Shape(Mathf.Lerp(1, 1.65f, t), Mathf.Lerp(1, .02f, t));
                portrait.enabled = t < 1;
                // Leave room for the player instead of respawning inside their controller.
                if (deathTimer >= respawnDelay && (target == null || Vector3.Distance(target.position, home) > 1.6f)) ResetAtHome();
                return;
            }

            flashTimer = Mathf.Max(0, flashTimer - dt);
            stunTimer = Mathf.Max(0, stunTimer - dt);
            landingTimer = Mathf.Max(0, landingTimer - dt);
            portrait.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", Color.white * (1 + 1.6f * Mathf.Clamp01(flashTimer / .16f)));
            portrait.SetPropertyBlock(properties);

            bool grounded = controller.isGrounded;
            if (grounded && !airborne)
            {
                verticalSpeed = -2;
                waitTimer -= dt;
                if (waitTimer <= 0 && stunTimer <= 0) BeginHop();
            }
            verticalSpeed -= gravity * dt;
            CollisionFlags collision = controller.Move((hopVelocity + recoil + Vector3.up * verticalSpeed) * dt);
            recoil = Vector3.MoveTowards(recoil, Vector3.zero, 8 * dt);
            if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if ((collision & CollisionFlags.Sides) != 0) hopVelocity = Vector3.zero;
            if ((collision & CollisionFlags.Below) != 0 && verticalSpeed <= 0)
            {
                if (airborne) { landingTimer = .18f; waitTimer = restDuration; }
                airborne = false;
                verticalSpeed = -2;
                hopVelocity = Vector3.zero;
            }

            if (airborne) Shape(.9f, verticalSpeed > 0 ? 1.17f : 1.08f);
            else
            {
                float squash = Mathf.Max(landingTimer / .18f, Mathf.Clamp01(1 - waitTimer / .22f));
                Shape(1 + .23f * squash, 1 - .27f * squash);
            }
            if (transform.position.y < home.y - 5) ResetAtHome();
        }

        private void BeginHop()
        {
            verticalSpeed = Mathf.Sqrt(2 * gravity * hopHeight);
            hopVelocity = Vector3.zero;
            if (target != null)
            {
                Vector3 offset = target.position - transform.position;
                float heightDifference = Mathf.Abs(offset.y);
                offset.y = 0;
                float distance = offset.magnitude;
                if (distance < detectionRange && distance > stopDistance && heightDifference < 2.2f)
                {
                    float flightTime = 2 * verticalSpeed / gravity;
                    hopVelocity = offset.normalized * Mathf.Min(hopSpeed, (distance - stopDistance) / flightTime);
                }
            }
            airborne = true;
            HopCount++;
        }

        public void ReceiveMagicHit(Vector3 direction)
        {
            if (defeated) return;
            RemainingHits--;
            flashTimer = .16f;
            stunTimer = .2f;
            recoil = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * 3.2f;
            hopVelocity = Vector3.zero;
            if (RemainingHits <= 0)
            {
                defeated = true;
                airborne = false;
                deathTimer = 0;
                controller.enabled = false;
            }
        }

        private void ResetAtHome()
        {
            controller.enabled = false;
            transform.position = home;
            controller.enabled = true;
            RemainingHits = hitPoints;
            defeated = airborne = false;
            verticalSpeed = 0;
            waitTimer = .45f;
            landingTimer = flashTimer = stunTimer = deathTimer = 0;
            hopVelocity = recoil = Vector3.zero;
            portrait.enabled = true;
            portrait.transform.localScale = restScale;
        }

        private void Shape(float width, float height)
            => portrait.transform.localScale = Vector3.Scale(restScale, new Vector3(width, height, 1));

        private void LateUpdate()
        {
            if (view == null || portrait == null) return;
            // Screen-aligned even at high camera pitch; squash and hop stay independent.
            portrait.transform.rotation = view.transform.rotation;
        }
    }
}
