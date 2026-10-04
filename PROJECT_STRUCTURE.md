# 项目结构说明

> 引擎版本：Unity 6000.3.25f1（URP）。日常开发内容全部放在 `Assets/Content/` 下，
> 本文件说明目录职责、脚本命名规则与事件总线的用法，方便新成员快速加入工作流。

## 一、目录结构

```text
YongXian/
├─ AGENTS.md                        协作者（含 AI）工作约定
├─ README.md
├─ PROJECT_STRUCTURE.md             本文件
├─ Packages/manifest.json           包依赖：URP、Input System、TMP、Test Framework 等
├─ ProjectSettings/                 Unity 工程设置（Build Settings、渲染、输入等）
└─ Assets/
   ├─ Content/                      ★ 所有开发内容
   │  ├─ Scenes/
   │  │  ├─ SampleScene.unity       主场景，所有 Prefab 都实例化在这里
   │  │  └─ Level_Test.unity        临时验证场景（可删）
   │  ├─ Prefabs/
   │  │  ├─ GameInstance.prefab     GameInstance + LevelManager + UIManager + InventoryManager
   │  │  ├─ Player.prefab           PlayerCharacter（persistent）
   │  │  ├─ Camera.prefab           CameraClass（persistent）
   │  │  └─ --Manager--.prefab      Level1GameMode（关卡玩法）
   │  ├─ Scripts/
   │  │  ├─ GameLogic/              纯逻辑，不依赖具体表现
   │  │  │  ├─ GameInstance.cs      总入口单例
   │  │  │  ├─ Singleton.cs         单例模板
   │  │  │  ├─ EventBus/
   │  │  │  │  ├─ EventBus.cs              事件的订阅 / 取消订阅 / 发布
   │  │  │  │  └─ GameEvent_Interface.cs   事件标记接口与事件结构体定义
   │  │  │  ├─ Manager/
   │  │  │  │  ├─ ManagerBase.cs           管理器基类（场景激活策略）
   │  │  │  │  ├─ GameModeBase.cs          关卡玩法基类
   │  │  │  │  ├─ LevelManager.cs          场景加载 / 卸载
   │  │  │  │  ├─ UIManager.cs
   │  │  │  │  ├─ InventoryManager.cs
   │  │  │  │  └─ LevelGameModes/Level1GameMode.cs
   │  │  │  └─ Player/
   │  │  │     ├─ PlayerCharacter.cs       玩家单例
   │  │  │     ├─ PlayerController.cs
   │  │  │     ├─ PlayerMove/              预留（空）
   │  │  │     ├─ Magic/                   预留（空）
   │  │  │     └─ Inventory/               预留（空）
   │  │  ├─ Presentation/           表现层：相机、特效、UI 表现
   │  │  │  └─ Camera/CameraClass.cs
   │  │  └─ test/                   临时调试脚本，不算正式逻辑
   │  │     └─ LoadsceneButton.cs   测试按钮：发布关卡切换事件
   │  ├─ GameData/                  预留：数值 / 配置资产（空）
   │  └─ Resources/                 预留：运行时动态加载的资源（空）
   ├─ Settings/                     URP 渲染管线与画质配置（PC / Mobile 的 RPAsset 与 Renderer）
   ├─ DefaultVolumeProfile.asset    URP 默认全局 Volume（Unity 生成，勿手动改）
   ├─ UniversalRenderPipelineGlobalSettings.asset  URP 全局设置（Unity 生成，勿手动改）
   ├─ Readme.asset                  Unity 模板自带，可忽略
   ├─ TextMesh Pro/                 TMP 必备资源，勿手动改
   ├─ TutorialInfo/                 Unity 模板自带说明页，可忽略
   ├─ MVP03/、MVP04/                历史里程碑原型，各自独立可运行，勿在其中改 Content
   └─ InputSystem_Actions.inputactions  输入动作资产
```

补充说明：

- `Scripts/GameLogic/Manager/LevelManagers/` 是重命名后遗留的空目录，可以删除。
- 每个资源旁边的 `.meta` 文件保存 Unity 的 GUID，**必须和资源一起提交**，不要单独删除或改名。
- 目前没有 `.asmdef`，所有脚本都编译进 `Assembly-CSharp`，新增脚本请放在 `Assets/Content/Scripts/` 下。

## 二、文件夹职责

| 路径 | 放什么 | 不放什么 |
| --- | --- | --- |
| `Content/Scenes` | 可运行场景 | 测试用临时对象 |
| `Content/Prefabs` | 跨场景复用、需要常驻的对象 | 只在一个场景用一次的临时对象 |
| `Content/Scripts/GameLogic` | 游戏规则、状态、管理器、事件 | 特效、动画、材质相关代码 |
| `Content/Scripts/Presentation` | 相机、特效、UI 等表现层代码 | 游戏规则判定 |
| `Content/Scripts/test` | 临时验证脚本 | 正式功能 |
| `Content/GameData`、`Content/Resources` | 配置资产、运行时加载资源 | 代码 |
| `Assets/Settings` | URP 管线与画质资产 | 业务资源 |

## 三、脚本命名规则

**文件与类型**

- 一个文件一个顶层类型，文件名与类型名完全一致（`LevelManager.cs` 里是 `class LevelManager`），这是 Unity 识别 MonoBehaviour 的硬性要求。
- 不使用命名空间，所有类型全局可见。

**大小写**

- 类型、方法、属性、枚举与枚举成员用 PascalCase：`ManagerBase`、`LevelLoad()`、`IsSwitchedOn`、`SceneActivationMode.Whitelist`。
- 字段、局部变量、参数用 camelCase，不加下划线前缀：`scriptList`、`activationMode`、`sceneNames`、`targetPosition`。

**后缀约定（本项目特有，请沿用）**

| 类别 | 规则 | 例子 |
| --- | --- | --- |
| 接口 | `Xxx_interface` | `Instance_interface`、`GameEvent_Interface` |
| 事件 | `XxxEvent` 结构体，实现 `GameEvent_Interface` | `LevelLoadEvent` |
| 管理器 | `XxxManager`，继承 `ManagerBase` | `LevelManager`、`UIManager` |
| 关卡玩法 | `LevelN+GameMode`，继承 `GameModeBase` | `Level1GameMode` |
| 单例 | 继承 `Singleton<T>`，类名即单例名 | `GameInstance`、`PlayerCharacter`、`CameraClass` |

**Unity 相关约定**

- Inspector 暴露的字段一律写成 `[SerializeField] private`，不要用 public 字段；用 `[Header("中文说明")]` 或 `[Tooltip("...")]` 标注含义。
- 继承 `Singleton<T>` 的类**不要覆盖 `Awake()`**，初始化写在 `OnSingletonAwake()`；需要清理时覆盖 `OnDestroy()` 并调用 `base.OnDestroy()`。
- 需要被 `GameInstance` 管理激活状态的组件，继承 `ManagerBase`，并和 `GameInstance` 挂在同一个 GameObject 上。

**格式**

- 4 空格缩进，大括号独占一行（Allman），`using` 放在文件顶部。
- 注释用中文单行 `//`，类与接口用块注释说明职责。
- 项目没有配置 linter / formatter，也没有 `.editorconfig`，请对齐周围代码风格。

## 四、事件总线（EventBus）

位置：`Assets/Content/Scripts/GameLogic/EventBus/`。

### 结构

```csharp
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> event_handlers;   // 事件类型 -> 处理函数列表

    public static void Subscribe<T>(Action<T> handler)   where T : GameEvent_Interface;  // 订阅（自动去重）
    public static void Unsubscribe<T>(Action<T> handler) where T : GameEvent_Interface;  // 取消订阅（列表空时清 key）
    public static void Publish<T>(T gameEvent)           where T : GameEvent_Interface;  // 发布（同步调用全部处理函数）
}
```

- 以事件**类型**为 key，一种事件类型只对应一个 `Action<T>` 签名。
- `Subscribe` 会跳过重复的同一个方法；`Unsubscribe` 在列表清空后移除该 key。
- 泛型约束 `where T : GameEvent_Interface`，只有实现了该标记接口的类型才能作为事件。

### 第一步：定义事件

在 `GameEvent_Interface.cs` 里新增结构体，用 `struct` + `[System.Serializable]`，字段 camelCase 并加 `[Header]` 方便在 Inspector 填写：

```csharp
[System.Serializable]
public struct LevelLoadEvent : GameEvent_Interface
{
    [Header("旧场景名称")]
    public string currName;
    [Header("新场景名称")]
    public string targetName;
    [Header("新场景中玩家位置")]
    public Vector3 targetPosition;
}
```

### 第二步：订阅 / 取消订阅

订阅和取消订阅**必须成对**，否则对象销毁后事件仍持有它的委托，下一次发布就会报错。推荐在单例的 `OnSingletonAwake()` 订阅、在 `OnDestroy()` 取消：

```csharp
protected override void OnSingletonAwake()
{
    EventBus.Subscribe<LevelLoadEvent>(OnLevelLoaded);
}

protected override void OnDestroy()
{
    EventBus.Unsubscribe<LevelLoadEvent>(OnLevelLoaded);
    base.OnDestroy();
}

void OnLevelLoaded(LevelLoadEvent levelLoadEvent)
{
    // 处理事件
}
```

### 第三步：发布

```csharp
public void CallLoad()
{
    EventBus.Publish<LevelLoadEvent>(levelLoadEvent);
}
```

`Assets/Content/Scripts/test/LoadsceneButton.cs` 就是最小示例：把它挂到 UI Button 上，Button 的 OnClick 绑定 `CallLoad()`，事件参数直接在 Inspector 里填。

### 现有事件：LevelLoadEvent

目前只有 `LevelLoadEvent` 一个事件，语义是"关卡切换请求"：

| 字段 | 含义 |
| --- | --- |
| `currName` | 旧场景名，切换完成后会被卸载 |
| `targetName` | 新场景名，会被加法加载（`LoadSceneMode.Additive`） |
| `targetPosition` | 玩家进入新场景后的世界坐标 |

完整链路：

1. 点击按钮 → `LoadsceneButton.CallLoad()` → `EventBus.Publish<LevelLoadEvent>(...)`。
2. `GameInstance.OnLevelLoaded` 收到事件：
   - 先 `ApplySceneActivation`：用 `targetName` 对挂在同一 GameObject 上的 `ManagerBase` 组件做激活切换（`AlwaysOn` 始终激活；白名单 / 黑名单按 `sceneNames` 判定；`targetName` 为空即纯卸载时不切换）。
   - 再调用 `LevelManager.LevelLoad`：异步加载 `targetName` → 加载完成后把 `PlayerCharacter.Instance` 移到 `targetPosition` → 最后卸载 `currName`。顺序不能颠倒，否则卸载最后一个已加载场景会被 Unity 拒绝。

### 注意事项

- `Publish` 是**同步**的，在当前调用栈里依次执行所有处理函数；某个处理函数抛异常会中断后面的处理（当前没有 try/catch 保护）。
- 事件是值类型，处理函数拿到的是副本，修改它不会影响发布方。
- 静态字典没有做"域重载重置"；如果在 Project Settings 里关掉了 Domain Reload（Enter Play Mode Options），上一次 Play 残留的订阅会保留下来，需要重新打开编辑器或补一个 `[RuntimeInitializeOnLoadMethod]` 清理。
- 事件本身不带发送者信息，需要区分来源时请在事件结构里自行加字段。

## 五、单例与管理器约定

- `Singleton<T>`：`Instance` 静态访问；`OnSingletonAwake()` 作为初始化入口；Inspector 勾选 `persistent` 决定是否 `DontDestroyOnLoad`；场景里出现重复实例会自动销毁并告警。
- `Instance_interface`：组件"可被 GameInstance 管理"的契约，成员为 `IsSwitchedOn`、`IsActiveInScene(string)`、`SwitchOn()`、`SwitchOff()`。
- `ManagerBase`：`Instance_interface` 的默认实现，自带 Inspector 配置 `activationMode`（始终激活 / 白名单 / 黑名单）与 `sceneNames` 列表；`SwitchOn/SwitchOff` 即 `enabled = true/false`。
- `GameModeBase` / `Level1GameMode`：存放当前关卡信息，负责关卡的开始、结束、暂停等通知。
