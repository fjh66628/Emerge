using UnityEngine;

namespace MVP03
{
    public sealed class MagicBolt : MonoBehaviour
    {
        [SerializeField] private MagicImpact impactPrefab;
        [SerializeField] private Transform glow;
        [SerializeField] private Transform orbit;
        [SerializeField] private TrailRenderer ribbon;
        [SerializeField] private ParticleSystem motes;
        [SerializeField] private Light glowLight;
        [SerializeField, Min(.1f), Tooltip("Initial flight speed in metres per second.")]
        private float speed = 9f;
        [SerializeField, Min(0), Tooltip("Speed gained per second; zero keeps constant flight speed.")]
        private float acceleration;
        [SerializeField, Min(.1f)] private float maxSpeed = 15f;
        [SerializeField, Min(.01f)] private float hitRadius = .16f;
        [SerializeField, Min(.1f)] private float lifetime = 2.5f;
        [SerializeField] private LayerMask collisionLayers = ~0;
        [SerializeField] private float glowDiameter = .60f, glowPulse = .025f;
        [SerializeField] private float lightIntensity = 5.5f, lightPulse = .7f;
        private Transform owner;
        private Vector3 direction;
        private float age;
        private bool launched, finished;
        public float CurrentSpeed => acceleration > 0
            ? Mathf.Min(speed + acceleration * age, Mathf.Max(speed, maxSpeed)) : speed;

        public void Configure(MagicImpact impact, Transform shell, Transform rings,
            TrailRenderer trail, ParticleSystem particles, Light pointLight)
        {
            impactPrefab = impact; glow = shell; orbit = rings;
            ribbon = trail; motes = particles; glowLight = pointLight;
        }

        public void Launch(Transform caster, Vector3 heading)
            => Launch(caster, heading, transform.position);

        public void ConfigureOrb(float diameter, float brightness, float travelSpeed)
        { glowDiameter = diameter; glowPulse = .012f; lightIntensity = brightness; lightPulse = .18f; speed = travelSpeed; }

        public void ConfigureAcceleration(float initialSpeed, float gainPerSecond, float terminalSpeed)
        { speed = Mathf.Max(.1f, initialSpeed); acceleration = Mathf.Max(0, gainPerSecond); maxSpeed = Mathf.Max(speed, terminalSpeed); }

        public void Launch(Transform caster, Vector3 heading, Vector3 castOrigin)
        {
            owner = caster;
            direction = heading.normalized;
            age = 0;
            launched = true;
            if (ribbon != null) ribbon.Clear();
            Vector3 destination = transform.position;
            transform.position = castOrigin;
            // SphereCast cannot report shapes already overlapping its origin.
            foreach (Collider obstacle in Physics.OverlapSphere(transform.position, hitRadius,
                         collisionLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsOwner(obstacle)) continue;
                Finish(transform.position, -direction, true, obstacle);
                return;
            }
            // The sigil sits in front of the actor: sweep to it instead of spawning through walls.
            Vector3 offset = destination - castOrigin;
            if (offset.sqrMagnitude > .000001f && Sweep(offset.normalized, offset.magnitude, out var hit))
            {
                Finish(hit.point, hit.normal, true, hit.collider);
                return;
            }
            transform.position = destination;
            if (ribbon != null) ribbon.Clear();
        }

        private bool IsOwner(Collider obstacle) => owner != null &&
            (obstacle.transform == owner || obstacle.transform.IsChildOf(owner));

        private void Update()
        {
            if (!launched || finished) return;
            float previousAge = age;
            age = Mathf.Min(age + Time.deltaTime, lifetime);
            // Integrate the capped acceleration exactly, including frames crossing the speed cap.
            float distance = FlightDistance(age) - FlightDistance(previousAge);
            if (Sweep(direction, distance, out var nearest))
            {
                transform.position += direction * nearest.distance;
                Finish(nearest.point, nearest.normal, true, nearest.collider);
                return;
            }
            transform.position += direction * distance;
            if (glow != null) glow.localScale = Vector3.one * (glowDiameter + Mathf.Sin(age * 23f) * glowPulse);
            if (orbit != null) orbit.Rotate(72f * Time.deltaTime, 135f * Time.deltaTime, 210f * Time.deltaTime, Space.Self);
            if (glowLight != null) glowLight.intensity = lightIntensity + Mathf.Sin(age * 19f) * lightPulse;
            if (age >= lifetime) Finish(transform.position, -direction, false);
        }

        private float FlightDistance(float elapsed)
        {
            if (acceleration <= 0) return speed * elapsed;
            float terminalSpeed = Mathf.Max(speed, maxSpeed);
            float acceleratingTime = Mathf.Min(elapsed, (terminalSpeed - speed) / acceleration);
            return speed * acceleratingTime + .5f * acceleration * acceleratingTime * acceleratingTime
                + terminalSpeed * (elapsed - acceleratingTime);
        }

        private bool Sweep(Vector3 heading, float distance, out RaycastHit nearest)
        {
            nearest = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.SphereCastAll(transform.position, hitRadius, heading,
                         distance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsOwner(hit.collider) || hit.distance >= nearestDistance) continue;
                nearest = hit; nearestDistance = hit.distance;
            }
            return nearestDistance < float.PositiveInfinity;
        }

        private void Finish(Vector3 point, Vector3 normal, bool hit, Collider obstacle = null)
        {
            if (finished) return;
            finished = true;
            if (hit && obstacle != null)
                obstacle.GetComponentInParent<IMagicHitReceiver>()?.ReceiveMagicHit(direction);
            if (hit && impactPrefab != null)
                Instantiate(impactPrefab, point + normal * .06f, Quaternion.LookRotation(normal));
            if (ribbon != null)
            {
                ribbon.transform.SetParent(null, true);
                ribbon.emitting = false;
                Destroy(ribbon.gameObject, ribbon.time + .05f);
            }
            if (motes != null)
            {
                motes.transform.SetParent(null, true);
                motes.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(motes.gameObject, Mathf.Max(.7f, motes.main.startLifetime.constantMax + .1f));
            }
            Destroy(gameObject);
        }
    }
}
