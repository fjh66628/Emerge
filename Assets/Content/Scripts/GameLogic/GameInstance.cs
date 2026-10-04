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

    //收到场景加载事件后先切换组件激活状态，再转交 LevelManager 执行
    public void OnLevelLoaded(LevelLoadEvent levelLoadEvent)
    {
        ApplySceneActivation(levelLoadEvent);

        var levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }

        levelManager.LevelLoad(levelLoadEvent);
    }

    //按事件里的新场景名切换各组件激活状态，纯卸载事件不切换
    void ApplySceneActivation(LevelLoadEvent levelLoadEvent)
    {
        if (string.IsNullOrEmpty(levelLoadEvent.targetName))
        {
            return;
        }

        foreach (var script in scriptList)
        {
            bool shouldActive = script.IsActiveInScene(levelLoadEvent.targetName);
            if (script.IsSwitchedOn == shouldActive)
            {
                continue;
            }

            script.UpdateScript();
        }
    }

}

public interface Instance_interface// 负责单例组件中生命周期的接口
{
    bool IsSwitchedOn { get; }// 当前是否处于激活状态
    bool IsActiveInScene(string sceneName);// 在指定场景名下是否应该激活
    void SwitchOn();
    void SwitchOff();

    void UpdateScript();// 更新脚本状态入口
}
