using UnityEngine;



public enum MagicReactionKind
{
    无反应,//只是记录，被撞物体不发生变化
    销毁自身,//被撞物体自己被销毁
    换成预制体,//被撞物体换成另一个预制体
}

[System.Serializable]
public class MagicReaction//Inspector里配置的一条反应：某种魔法撞上来要做什么
{
    [SerializeField] private MagicType magicType;
    public MagicType GetMagicType => magicType;

    [SerializeField] private MagicReactionKind reactionKind;
    public MagicReactionKind GetReactionKind => reactionKind;

    [SerializeField] private GameObject replacementPrefab;//反应为“换成预制体”时使用
    public GameObject GetReplacementPrefab => replacementPrefab;

    [SerializeField] private GameObject vfxPrefab;//命中特效，可留空
    public GameObject GetVfxPrefab => vfxPrefab;
}

