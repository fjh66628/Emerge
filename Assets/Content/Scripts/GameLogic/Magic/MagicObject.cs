using UnityEngine;

public class MagicObject : MonoBehaviour
{
    [SerializeField] private MagicList magicList;//策划配表，在预制体上配置

    private MagicClassBase magicClass;//本次魔法的基础数值
    public MagicClassBase GetMagicClass => magicClass;
    public MagicType GetMagicType { get; private set; }//本次魔法的类型，配表缺条目时也能知道类型
    public Transform GetCaster { get; private set; }//施法者，魔法不与它及其子物体碰撞

    //由 UseMagic 调用：按类型取配表数值，并把移动参数交给 MagicMove
    public void Init(MagicType magicType, MagicMoveType moveType, float speed, float lifeTime, Transform caster)
    {
        GetMagicType = magicType;
        GetCaster = caster;
        magicClass = CreateMagic(magicType);

        MagicMove magicMove = GetComponent<MagicMove>();
        if (magicMove == null)
        {
            Debug.LogWarning("MagicObject: 预制体上没有 MagicMove，魔法不会移动。", this);
            return;
        }

        magicMove.Init(moveType, speed, lifeTime);
    }

    //按魔法类型构造对应的数据类
    MagicClassBase CreateMagic(MagicType magicType)
    {
        if (magicList == null)
        {
            Debug.LogError("MagicObject: magicList 未配置，读不到配表数值。", this);
            return null;
        }

        switch (magicType)
        {
            case MagicType.水: return new WaterMagic(magicList);
            case MagicType.火: return new FireMagic(magicList);
            case MagicType.岩: return new RockMagic(magicList);
            case MagicType.气: return new AirMagic(magicList);
            case MagicType.烤: return new CookMagic(magicList);
            case MagicType.融: return new MeltMagic(magicList);
            default:
                Debug.LogWarning("MagicObject: 配表中没有对应类型 " + magicType + "，数值为空。", this);
                return null;
        }
    }
}
