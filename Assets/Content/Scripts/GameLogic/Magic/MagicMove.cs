using UnityEngine;

public class MagicMove : MonoBehaviour
{
    private MagicMoveType moveType;//本次魔法的移动方式
    private float speed;//飞行速度
    private float lifeTime;//存活时间，到点自动销毁

    //由 MagicObject 调用：设置移动方式与移动参数
    public void Init(MagicMoveType magicMoveType, float magicSpeed, float magicLifeTime)
    {
        moveType = magicMoveType;
        speed = magicSpeed;
        lifeTime = magicLifeTime;

        Destroy(gameObject, lifeTime);//超时自动销毁
    }

    //根据移动方式驱动魔法飞行
    void Update()
    {
        switch (moveType)
        {
            case MagicMoveType.直线:
                transform.position += transform.forward * speed * Time.deltaTime;
                break;
            case MagicMoveType.追踪:
                //追踪逻辑以后再写
                break;
            case MagicMoveType.原地:
                //原地停留，以后接范围效果
                break;
        }
    }
}
