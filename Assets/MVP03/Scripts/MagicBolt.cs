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
        [SerializeField, Min(.1f)] private float speed = 9f;
        [SerializeField, Min(.01f)] private float hitRadius = .16f;
        [SerializeField, Min(.1f)] private float lifetime = 2.5f;
        [SerializeField] private LayerMask collisionLayers = ~0;
        [SerializeField] private float glowDiameter = .60f, glowPulse = .025f;
        [SerializeField] private float lightIntensity = 5.5f, lightPulse = .7f;
        private Transform owner;
        private Vector3 direction;
        private float age;
        private bool launched, finished;

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

        public void Launch(Transform caster, Vector3 heading, Vector3 castOrigin)
        {
            owner = caster;
            direction = heading.normalized;
            launched = true;
            if (ribbon != null) ribbon.Clear();
            Vector3 destination = transform.position;
            transform.position = castOrigin;
            // SphereCast cannot report shapes already overlapping its origin.
            foreach (Collider obstacle in Physics.OverlapSphere(transform.position, hitRadius,
                         collisionLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsOwner(obstacle)) continue;
                Finish(transform.position, -direction, true);
                return;
            }
            // The sigil sits in front of the actor: sweep to it instead of spawning through walls.
            Vector3 offset = destination - castOrigin;
            if (offset.sqrMagnitude > .000001f && Sweep(offset.normalized, offset.magnitude, out var hit))
            {
                Finish(hit.point, hit.normal, true);
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
            age += Time.deltaTime;
            float distance = speed * Time.deltaTime;
            if (Sweep(direction, distance, out var nearest))
            {
                transform.position += direction * nearest.distance;
                Finish(nearest.point, nearest.normal, true);
                return;
            }
            transform.position += direction * distance;
            if (glow != null) glow.localScale = Vector3.one * (glowDiameter + Mathf.Sin(age * 23f) * glowPulse);
            if (orbit != null) orbit.Rotate(72f * Time.deltaTime, 135f * Time.deltaTime, 210f * Time.deltaTime, Space.Self);
            if (glowLight != null) glowLight.intensity = lightIntensity + Mathf.Sin(age * 19f) * lightPulse;
            if (age >= lifetime) Finish(transform.position, -direction, false);
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

        private void Finish(Vector3 point, Vector3 normal, bool hit)
        {
            if (finished) return;
            finished = true;
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
