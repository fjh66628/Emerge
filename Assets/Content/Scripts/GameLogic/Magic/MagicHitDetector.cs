using UnityEngine;

public class MagicHitDetector : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.15f;//扫掠球半径，太小会穿模，太大会提前蹭到东西

    private MagicObject magicObject;//同物体上的魔法数据，命中时从中取魔法类型与配表数值
    private Vector3 lastPosition;//上一帧的位置，用来算本帧位移
    private bool finished;//已经命中过，同一帧内不再重复触发

    void Awake()
    {
        magicObject = GetComponent<MagicObject>();
        if (magicObject == null)
        {
            Debug.LogError("MagicHitDetector: 同物体上没有 MagicObject，读不到魔法类型。", this);
        }
    }

    //出生帧：先查一次是否已经和别的物体重叠（例如出生点贴在墙里），再记录位置
    void Start()
    {
        lastPosition = transform.position;
        CheckSpawnOverlap();
    }

    //放在 LateUpdate：等 MagicMove 移动完，再用本帧位移做扫掠
    void LateUpdate()
    {
        if (finished)
        {
            return;
        }

        Vector3 position = transform.position;

        Vector3 offset = position - lastPosition;
        float distance = offset.magnitude;

        if (distance <= 0f)
        {
            return;//本帧没有位移（例如原地停留），不需要扫掠
        }

        Vector3 direction = offset / distance;
        if (Sweep(direction, distance, out RaycastHit nearest))
        {
            transform.position = lastPosition + direction * nearest.distance;//停在命中点上
            Hit(nearest.collider, nearest.point, nearest.normal, direction);
            return;
        }

        lastPosition = position;
    }

    //出生点已经重叠在别的碰撞体里时直接命中，施法者与魔法自己不算
    void CheckSpawnOverlap()
    {
        if (finished)
        {
            return;
        }

        Vector3 origin = transform.position;
        Vector3 point = origin;
        Collider nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (Collider other in Physics.OverlapSphere(origin, hitRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (IsSelf(other) || IsCaster(other))
            {
                continue;
            }

            Vector3 closest = other.ClosestPointOnBounds(origin);
            float distance = Vector3.Distance(origin, closest);
            if (distance >= nearestDistance)
            {
                continue;
            }
            nearest = other;
            nearestDistance = distance;
            point = closest;
        }

        if (nearest == null)
        {
            return;
        }

        Vector3 normal = origin - point;
        Vector3 direction = transform.forward;
        Hit(nearest, point, normal.sqrMagnitude > 0f ? normal.normalized : -direction, direction);
    }

    //沿本帧位移扫掠，取最近的实体碰撞体（Trigger 不算，魔法直接穿过；施法者与魔法自己也不算）
    bool Sweep(Vector3 direction, float distance, out RaycastHit nearest)
    {
        nearest = default;
        float nearestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in Physics.SphereCastAll(lastPosition, hitRadius, direction,
                     distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (IsSelf(hit.collider) || IsCaster(hit.collider) || hit.distance >= nearestDistance)
            {
                continue;
            }
            nearest = hit;
            nearestDistance = hit.distance;
        }

        return nearestDistance < float.PositiveInfinity;
    }

    //施法者 = 挂了 UseMagic 的物体及其所有子物体，它的碰撞体不会触发魔法的销毁
    bool IsCaster(Collider other)
    {
        Transform caster = magicObject != null ? magicObject.GetCaster : null;
        return caster != null && (other.transform == caster || other.transform.IsChildOf(caster));
    }

    //魔法自己身上的碰撞体也不算命中
    bool IsSelf(Collider other)
    {
        return other.transform == transform || other.transform.IsChildOf(transform);
    }

    //撞到东西：通知被撞物体，然后销毁魔法
    void Hit(Collider obstacle, Vector3 point, Vector3 normal, Vector3 direction)
    {
        if (finished)
        {
            return;
        }
        finished = true;

        MagicInteractable interactable = obstacle.GetComponentInParent<MagicInteractable>();
        if (interactable != null && magicObject != null)
        {
            MagicHitInfo hitInfo = new MagicHitInfo
            {
                magicType = magicObject.GetMagicType,
                magicClass = magicObject.GetMagicClass,
                point = point,
                normal = normal,
                direction = direction,
            };
            interactable.OnMagicHit(hitInfo);
        }

        Destroy(gameObject);//命中后无条件销毁，以后要加“是否消耗魔法”就改这里
    }
}
