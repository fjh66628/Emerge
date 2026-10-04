using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * 关卡管理器
 负责关卡的加载、切换、重置等操作
 */
public class LevelManager : ManagerBase
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //实现场景加载逻辑：targetName 是新场景，levelName 是旧场景
    public void LevelLoad(LevelLoadEvent levelLoadEvent)
    {
        //targetName 为空表示本次只做卸载
        if (string.IsNullOrEmpty(levelLoadEvent.targetName))
        {
            UnloadLevelScene(levelLoadEvent.currName);
            return;
        }

        //先异步加载新场景
        AsyncOperation operation = SceneManager.LoadSceneAsync(levelLoadEvent.targetName, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogError($"场景加载失败：{levelLoadEvent.targetName}");
            return;
        }

        //加载完成后先摆放玩家，再卸载旧场景，避免卸载最后一个已加载场景被 Unity 拒绝
        operation.completed += _ =>
        {
            if (PlayerCharacter.Instance == null)
            {
                Debug.LogError("PlayerCharacter not found!");
            }
            else
            {
                PlayerCharacter.Instance.transform.position = levelLoadEvent.targetPosition;
            }

            UnloadLevelScene(levelLoadEvent.currName);
        };
    }

    //卸载指定场景
    void UnloadLevelScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (targetScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(targetScene);
        }
    }
}
