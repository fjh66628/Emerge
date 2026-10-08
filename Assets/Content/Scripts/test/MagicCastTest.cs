using UnityEngine;

public class MagicCastTest : MonoBehaviour
{
    [SerializeField] private UseMagic useMagic;//临时测试用：被调用的魔法释放组件
    [SerializeField] private MagicType magicType = MagicType.水;//测试用魔法类型
    [SerializeField] private MagicMoveType moveType = MagicMoveType.直线;//测试用移动方式

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (useMagic == null)
        {
            Debug.LogWarning("MagicCastTest: useMagic 未赋值。", this);
            return;
        }

        useMagic.Cast(magicType, moveType);
    }
}
