using UnityEngine;

namespace MVP03
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class CourtyardEnemySpawner : MonoBehaviour
    {
        [SerializeField] private BouncingSlime enemyPrefab;
        [SerializeField] private PixelPilgrim player;
        private int sequence;

        public void Configure(BouncingSlime prefab, PixelPilgrim hero)
        { enemyPrefab = prefab; player = hero; }

        public bool TrySpawn(out BouncingSlime enemy, out string message)
        {
            enemy = null;
            if (!Application.isPlaying || enemyPrefab == null)
            { message = "敌人生成器尚未配置"; return false; }
            if (player == null) player = FindFirstObjectByType<PixelPilgrim>();
            if (player == null) { message = "没有找到主角"; return false; }

            var body = enemyPrefab.GetComponent<CharacterController>();
            var view = GetComponent<Camera>();
            Vector3 forward = Vector3.ProjectOnPlane(player.FacingDirection, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Physics.SyncTransforms();
            // Search successive rings, preferring the player's facing direction.
            for (int ring = 0; ring < 3; ring++) for (int sample = 0; sample < 16; sample++)
            {
                float angle = sample * 22.5f + sequence * 45f;
                Vector3 candidate = player.transform.position + Quaternion.AngleAxis(angle, Vector3.up) * forward * (2.6f + ring * 1.4f);
                if (!TryGround(candidate, out var ground)) continue;
                float baseOffset = body.center.y - body.height * .5f;
                Vector3 position = ground.point + Vector3.up * (.06f - baseOffset);
                Vector3 centre = position + body.center;
                Vector3 halfSegment = Vector3.up * Mathf.Max(0, body.height * .5f - body.radius);
                if (Physics.CheckCapsule(centre - halfSegment, centre + halfSegment, body.radius + .03f,
                    ~0, QueryTriggerInteraction.Ignore)) continue;
                if (!Supported(ground.point, body.radius * .75f)) continue;
                Vector3 screen = view.WorldToViewportPoint(centre);
                if (screen.z <= 0 || screen.x < .06f || screen.x > .94f || screen.y < .08f || screen.y > .92f) continue;
                Vector3 eye = player.transform.position + Vector3.up * .75f;
                Vector3 sight = centre - eye;
                bool blocked = false;
                foreach (var hit in Physics.RaycastAll(eye, sight.normalized, sight.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(player.transform)) { blocked = true; break; }
                if (blocked) continue;

                enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
                enemy.name = "BlueSlime / spawned " + ++sequence;
                enemy.Configure(enemy.GetComponentInChildren<SpriteRenderer>(), player.transform, view);
                message = "已生成蓝色史莱姆";
                return true;
            }
            message = "附近没有空位，请移动后重试";
            return false;
        }

        private bool TryGround(Vector3 candidate, out RaycastHit hit)
        {
            if (!Physics.Raycast(candidate + Vector3.up * 2, Vector3.down, out hit, 4,
                ~0, QueryTriggerInteraction.Ignore)) return false;
            return hit.normal.y >= .71f && Mathf.Abs(hit.point.y - player.transform.position.y) < 1.2f
                && hit.collider is not CharacterController && hit.collider.attachedRigidbody == null;
        }

        private bool Supported(Vector3 centre, float radius)
        {
            for (int side = 0; side < 4; side++)
            {
                Vector3 probe = centre + Quaternion.AngleAxis(side * 90, Vector3.up) * Vector3.forward * radius;
                if (!TryGround(probe, out var support) || Mathf.Abs(support.point.y - centre.y) > .3f) return false;
            }
            return true;
        }
    }
}
