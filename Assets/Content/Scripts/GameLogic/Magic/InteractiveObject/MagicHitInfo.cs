using UnityEngine;

public struct MagicHitInfo//一次魔法命中的信息，交给被撞物体决定发生什么
{
    public MagicType magicType;//本次魔法的类型，配表缺条目时也能知道类型
    public MagicClassBase magicClass;//本次魔法的配表数值，配表里没有对应类型时为 null
    public Vector3 point;//命中点
    public Vector3 normal;//命中面法线
    public Vector3 direction;//魔法飞行方向
}
