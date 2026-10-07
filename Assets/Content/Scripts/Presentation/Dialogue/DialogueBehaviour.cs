using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;//显示对话文字的 UI 组件
    [SerializeField] private Image image;//对话框（Image 所在物体负责显示和隐藏）

    private List<string> lines = new List<string>();//当前正在播放的对话内容
    private int currentIndex = -1;//当前句子的索引，-1 表示没有在对话中

    void OnEnable()
    {
        EventBus.Subscribe<DialogueAdvanceEvent>(OnDialogueAdvance);//订阅玩家推进对话的输入事件
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<DialogueAdvanceEvent>(OnDialogueAdvance);//解订阅，避免事件总线留下失效的处理函数
    }

    //收到玩家输入事件后跳转下一句
    void OnDialogueAdvance(DialogueAdvanceEvent dialogueAdvanceEvent)
    {
        AdvanceDialogue();
    }

    //由 UIManager 调用：接收对话数据、显示对话背景和文字
    public void BeginDialogue(List<string> dialogueLines)
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogWarning("DialogueBehaviour: 对话数据为空，无法开始对话。", this);
            return;
        }

        if (image == null)
        {
            Debug.LogError("DialogueBehaviour: image 未赋值，无法显示对话。", this);
            return;
        }

        lines = new List<string>(dialogueLines);//拷贝一份，避免结束时清空对话数据资产
        currentIndex = 0;

        image.gameObject.SetActive(true);//激活对话框背景和文字
        ChangeLines();
    }

    //玩家输入的入口：跳转下一句，已经在最后一句时结束对话
    public void AdvanceDialogue()
    {
        if (currentIndex < 0 || currentIndex >= lines.Count)
        {
            return;//没有在对话中，忽略本次输入
        }

        if (currentIndex + 1 < lines.Count)
        {
            currentIndex++;
            ChangeLines();
        }
        else
        {
            EndDialogue();
        }
    }

    //切换句子的时候的表现
    void ChangeLines()
    {
        text.text = lines[currentIndex];
    }

    //结束对话：重置数据并隐藏对话
    void EndDialogue()
    {
        currentIndex = -1;
        lines.Clear();
        image.gameObject.SetActive(false);
    }
}
