using UnityEngine;

public interface GameEvent_Interface
{
}

public struct LevelLoadEvent : GameEvent_Interface
{
    public string levelName;
    public string TargetName;
    public Vector3 TargetPosition;
}