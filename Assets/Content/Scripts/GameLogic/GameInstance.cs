using UnityEngine;
using System.Collections.Generic;
/*
    负责游戏的初始化、运行、暂停、结束等操作
    管理玩家状态
*/

public class GameInstance : Singleton<GameInstance>
{
    List<Instance_interface> scriptList = new List<Instance_interface>();
    protected override void OnSingletonAwake()
    {
        foreach (var script in GetComponents<Instance_interface>())
        {
            scriptList.Add(script);
        }

        EventBus.Subscribe<LevelLoadEvent>(OnLevelLoaded);// 订阅场景加载事件
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<LevelLoadEvent>(OnLevelLoaded);// 取消订阅场景加载事件

        base.OnDestroy();
    }

    //收到场景加载事件后转交 LevelManager 执行
    public void OnLevelLoaded(LevelLoadEvent levelLoadEvent)
    {
        var levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }

        levelManager.LevelLoad(levelLoadEvent);
    }

}

public interface Instance_interface// 负责单例组件中生命周期的接口
{
    void SwitchOn();
    void SwitchOff();
}
