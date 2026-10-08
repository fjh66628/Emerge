using UnityEngine;
using System.Collections.Generic;

public enum ObjectType
{
    玩家,
    敌人,
    物品,
}

public class MagicInteractable : MonoBehaviour//可以和魔法交互的对象
{
    [SerializeField] protected ObjectType objectType;

    [Header("魔法反应表")]
    [SerializeField] private MagicReactionList reactions;//策划配置：某种魔法撞上来要做什么

    //被魔法命中时由 MagicHitDetector 调用：默认按反应表处理，子类可以重写补充自己的逻辑
    public virtual void OnMagicHit(MagicHitInfo hit)
    {
        MagicReaction reaction = FindReaction(hit.magicType);
        Debug.Log($"OnMagicHit: {hit.magicType} {reaction}");
        if (reaction == null)
        {
            return;//反应表里没配这个魔法类型，被撞物体不发生变化
        }

        PlayVfx(reaction, hit);

        switch (reaction.GetReactionKind)
        {
            case MagicReactionKind.无反应:
                break;
            case MagicReactionKind.销毁自身:
                Destroy(gameObject);
                break;
            case MagicReactionKind.换成预制体:
                ReplaceSelf(reaction);
                break;
        }
    }

    //取反应表里第一条匹配该魔法类型的条目，同类型配了多条只有第一条生效
    MagicReaction FindReaction(MagicType magicType)
    {
        foreach (MagicReaction reaction in reactions.GetReactions)
        {
            if (reaction != null && reaction.GetMagicType == magicType)
            {
                return reaction;
            }
        }
        return null;
    }

    //命中特效放在命中点上，朝向命中面法线
    void PlayVfx(MagicReaction reaction, MagicHitInfo hit)
    {
        if (reaction.GetVfxPrefab == null)
        {
            return;
        }
        Instantiate(reaction.GetVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
    }

    //换成预制体：沿用自身的父级、位置与旋转，然后销毁自己
    void ReplaceSelf(MagicReaction reaction)
    {
        if (reaction.GetReplacementPrefab == null)
        {
            Debug.LogWarning("MagicInteractable: 反应为换成预制体，但没有配置替换预制体。", this);
            return;
        }
        Instantiate(reaction.GetReplacementPrefab, transform.position, transform.rotation, transform.parent);
        Destroy(gameObject);
    }
}
