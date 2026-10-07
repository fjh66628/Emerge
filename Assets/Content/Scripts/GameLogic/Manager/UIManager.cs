using UnityEngine;

public class UIManager : ManagerBase
{
    [SerializeField] DialogueBehaviour dialogueBehaviour;

    //显示对话：把对话数据交给对话表现层处理
    public void ShowDialogue(DialogueList dialogueList)
    {
        if (dialogueBehaviour == null)
        {
            Debug.LogError("UIManager: dialogueBehaviour 未赋值，无法显示对话。", this);
            return;
        }

        if (dialogueList == null)
        {
            Debug.LogError("UIManager: 对话数据为空，无法显示对话。", this);
            return;
        }

        dialogueBehaviour.BeginDialogue(dialogueList.GetDialogues);
    }
}
