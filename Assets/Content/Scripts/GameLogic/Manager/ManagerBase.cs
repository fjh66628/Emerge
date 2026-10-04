using System.Collections.Generic;
using UnityEngine;

public enum SceneActivationMode
{
    AlwaysOn,// 始终激活
    Whitelist,// 名单内场景激活
    Blacklist// 名单内场景停用
}

/*
 * 管理器基类
 负责按关卡加载事件中的场景名切换组件激活状态
 */
public abstract class ManagerBase : MonoBehaviour, Instance_interface
{
    [SerializeField]
    private SceneActivationMode activationMode = SceneActivationMode.AlwaysOn;// 默认始终激活，避免误停用

    [SerializeField]
    private List<string> sceneNames = new List<string>();// 白名单/黑名单场景名

    //当前组件是否处于激活状态
    public bool IsSwitchedOn => this.enabled;

    //判断该组件在指定场景名是否应该激活
    bool IsActiveInScene(string sceneName)
    {
        switch (activationMode)
        {
            case SceneActivationMode.Whitelist:
                return sceneNames.Contains(sceneName);
            case SceneActivationMode.Blacklist:
                return !sceneNames.Contains(sceneName);
            default:
                return true;
        }
    }

    public virtual void SwitchOn()
    {
        this.enabled = true;
    }

    public virtual void SwitchOff()
    {
        this.enabled = false;
    }

    public virtual void UpdateScript()
    {
        // 根据当前场景名切换组件激活状态
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool shouldActive = IsActiveInScene(currentSceneName);

        if (IsSwitchedOn != shouldActive)
        {
            if (shouldActive)
            {
                SwitchOn();
            }
            else
            {
                SwitchOff();
            }
        }
    }
}
