using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;//输入资源：Assets/InputSystem_Actions.inputactions
    [SerializeField] private string advanceActionPath = "Player/Next";//跳转下一句使用的动作路径

    private InputAction advanceAction;

    void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("DialogueInputHandler: 输入资源未配置，对话输入不会生效。", this);
            return;
        }

        advanceAction = inputActions.FindAction(advanceActionPath);
        if (advanceAction == null)
        {
            Debug.LogWarning("DialogueInputHandler: 找不到动作 " + advanceActionPath + "，对话输入不会生效。", this);
            return;
        }

        advanceAction.performed += OnAdvancePerformed;
        advanceAction.Enable();
    }

    void OnDisable()
    {
        if (advanceAction == null)
        {
            return;
        }

        advanceAction.performed -= OnAdvancePerformed;
        advanceAction.Disable();
        advanceAction = null;
    }

    //收到玩家输入后只发事件，不直接引用对话模块
    void OnAdvancePerformed(InputAction.CallbackContext context)
    {
        EventBus.Publish(new DialogueAdvanceEvent());
    }
}
