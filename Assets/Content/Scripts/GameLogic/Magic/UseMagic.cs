using UnityEngine;

public class UseMagic : MonoBehaviour
{
    [SerializeField] private MagicObject magicPrefab;//通用魔法预制体
    [SerializeField] private float defaultSpeed = 40f;//默认飞行速度，测试用
    [SerializeField] private float defaultLifeTime = 2f;//默认存活时间，测试用

    //供角色类调用：实例化一个魔法
    public void Cast(MagicType magicType, MagicMoveType moveType)
    {
        if (magicPrefab == null)
        {
            Debug.LogError("UseMagic: magicPrefab 未配置，无法释放魔法。", this);
            return;
        }

        Vector3 direction = GetCastDirection();
        MagicObject magic = Instantiate(magicPrefab, transform.position, Quaternion.LookRotation(direction, Vector3.up));
        magic.Init(magicType, moveType, GetSpeed(), GetLifeTime(), transform);
    }

    //获取当前释放方向，目前先固定返回 x 正方向，以后接角色朝向
    public Vector3 GetCastDirection()
    {
        return Vector3.right;
    }

    //计算本次魔法的飞行速度，目前先返回默认值，以后接计算逻辑
    float GetSpeed()
    {
        return defaultSpeed;
    }

    //计算本次魔法的存活时间，目前先返回默认值，以后接计算逻辑
    float GetLifeTime()
    {
        return defaultLifeTime;
    }
}
