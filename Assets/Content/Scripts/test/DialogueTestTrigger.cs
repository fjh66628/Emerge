using UnityEngine;

public class DialogueTestTrigger : MonoBehaviour
{
    [SerializeField] private DialogueComponent dialogueComponent;//临时测试用：在场景启动时触发一次对话

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (dialogueComponent == null)
        {
            Debug.LogWarning("DialogueTestTrigger: dialogueComponent 未赋值。", this);
            return;
        }

        dialogueComponent.ShowDialogue();
    }
}
