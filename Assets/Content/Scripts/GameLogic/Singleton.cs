using UnityEngine;

/// <summary>
/// MonoBehaviour 单例模板，用法：class GameManager : Singleton&lt;GameManager&gt;。
/// 初始化逻辑请写在 OnSingletonAwake 中，不要覆盖 Awake。
/// </summary>
[DisallowMultipleComponent]
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;

    /// <summary>场景中的实例；不存在或已销毁时返回 null。</summary>
    public static T Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }


            instance = FindInstance();
            return instance;
        }
    }

    /// <summary>实例是否已存在（不会触发查找）。</summary>
    public static bool IsInitialized => instance != null;

    [SerializeField]
    [Tooltip("切换场景时保留该单例")]
    private bool persistent;

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[Singleton] 场景中已存在 {typeof(T).Name}，销毁重复对象 {name}。", this);
            Destroy(gameObject);
            return;
        }

        instance = (T)this;

        if (persistent)
        {
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name} 不是根节点，无法跨场景保留。", this);
            }
        }

        OnSingletonAwake();
    }

    protected virtual void OnDestroy()
    {
        if (instance == (T)this)
        {
            instance = null;
        }
    }

    /// <summary>单例就绪后的初始化入口，替代 Awake 使用。</summary>
    protected virtual void OnSingletonAwake()
    {
    }

    private static T FindInstance()
    {
        return FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }


}