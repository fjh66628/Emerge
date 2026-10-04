using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * 关卡管理器
 负责关卡的加载、切换、重置等操作
 */
public class LevelManager : MonoBehaviour, Instance_interface
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //实现场景加载逻辑
    public void LevelLoad(LevelLoadEvent levelLoadEvent)
    {
        //TargetName 有值则卸载对应场景
        if (!string.IsNullOrEmpty(levelLoadEvent.TargetName))
        {
            Scene targetScene = SceneManager.GetSceneByName(levelLoadEvent.TargetName);
            if (targetScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(targetScene);
            }
        }

        //levelName 为空表示本次只做卸载
        if (string.IsNullOrEmpty(levelLoadEvent.levelName))
        {
            return;
        }

        //异步加载场景
        AsyncOperation operation = SceneManager.LoadSceneAsync(levelLoadEvent.levelName, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogError($"场景加载失败：{levelLoadEvent.levelName}");
            return;
        }

        //加载完成后把玩家角色放到事件指定的位置
        operation.completed += _ =>
        {
            if (PlayerCharacter.Instance == null)
            {
                Debug.LogError("PlayerCharacter not found!");
                return;
            }

            PlayerCharacter.Instance.transform.position = levelLoadEvent.TargetPosition;
        };
    }

    /*
        <summary>接口实现部分
    */
    public void SwitchOn()
    {
        this.enabled = true;
    }

    public void SwitchOff()
    {
        this.enabled = false;
    }
}
