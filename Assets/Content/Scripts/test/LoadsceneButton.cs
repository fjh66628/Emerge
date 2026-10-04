using UnityEngine;

public class LoadsceneButton : MonoBehaviour
{
    [SerializeField] LevelLoadEvent levelLoadEvent;
    public void CallLoad()
    {
        EventBus.Publish<LevelLoadEvent>(levelLoadEvent);
    }
}
