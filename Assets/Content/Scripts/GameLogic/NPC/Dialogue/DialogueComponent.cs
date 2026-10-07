using UnityEngine;

public class DialogueComponent : MonoBehaviour
{
    [Header("对话数据")]
    [SerializeField] private DialogueList dialogues;//此处存放对话数据

    [SerializeField] UIManager theUIManager;

    //触发对话：由 NPC 交互或者其他系统调用
    public void ShowDialogue()
    {
        if (dialogues == null)
        {
            Debug.LogWarning("DialogueComponent: 没有配置对话数据，无法显示对话。", this);
            return;
        }

        if (theUIManager == null)
        {
            Debug.LogError("DialogueComponent: theUIManager 未赋值，无法显示对话。", this);
            return;
        }

        theUIManager.ShowDialogue(dialogues);
    }
}
