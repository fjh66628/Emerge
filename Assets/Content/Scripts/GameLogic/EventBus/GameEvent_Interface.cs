using UnityEngine;

public interface GameEvent_Interface
{
}

[System.Serializable]
public struct LevelLoadEvent : GameEvent_Interface
{
    [Header("旧场景名称")]
    public string currName;//旧场景
    [Header("新场景名称")]
    public string targetName;//新场景
    [Header("新场景中玩家位置")]
    public Vector3 targetPosition;
}

[System.Serializable]
public struct DialogueAdvanceEvent : GameEvent_Interface
{
    //玩家输入要求跳转下一句对话，本身不需要携带数据
}
