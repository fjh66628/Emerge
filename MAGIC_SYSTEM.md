# 魔法系统说明（数据结构 / 配表 / 调用链）

> 适用范围：`Assets/Content/` 下的魔法玩法代码与配表。本文说明数据结构怎么工作、配表怎么配、代码怎么构造与调用。

## 脚本一览

| 脚本 | 位置（相对 `Assets/Content/Scripts/`） | 作用 |
| --- | --- | --- |
| `UseMagic` | `GameLogic/Magic/UseMagic.cs` | 挂在施法者身上，对外唯一释放入口 `Cast(MagicType, MagicMoveType)`：算方向/速度/存活时间，实例化魔法预制体，并把施法者自己传进去 |
| `MagicObject` | `GameLogic/Magic/MagicObject.cs` | 挂在魔法预制体上：按类型查配表构造数据类，持有本次数值与施法者，把移动参数转交 `MagicMove` |
| `MagicMove` | `GameLogic/Magic/MagicMove.cs` | 挂在同一个预制体上：按 `MagicMoveType` 驱动飞行（目前只实现直线），`Init` 时注册超时自毁 |
| `MagicMoveType` | `GameLogic/Magic/MagicMoveType.cs` | 枚举：直线 / 追踪 / 原地 |
| `MagicHitDetector` | `GameLogic/Magic/MagicHitDetector.cs` | 挂在魔法预制体上：出生帧 `OverlapSphere` + 之后每帧按位移 `SphereCast` 扫掠；撞到实体就停在命中点、通知 `MagicInteractable`、销毁魔法；施法者（含子物体）、魔法自己、Trigger 都不算命中 |
| `MagicInteractable` | `GameLogic/Magic/InteractiveObject/MagicInteractable.cs` | 挂在可交互场景物体上：被命中时 `OnMagicHit(MagicHitInfo)` 按反应表处理，子类可重写补充自己的逻辑 |
| `MagicReaction` / `MagicReactionKind` | `GameLogic/Magic/InteractiveObject/MagicReaction.cs` | 反应表里的一条：魔法类型 + 反应种类（无反应 / 销毁自身 / 换成预制体）+ 替换预制体 + 命中特效 |
| `MagicReactionList` | `GameLogic/Magic/InteractiveObject/MagicReactionList.cs` | ScriptableObject 反应表（一组 `MagicReaction`），`MagicInteractable.reactions` 指向它 |
| `MagicHitInfo` | `GameLogic/Magic/InteractiveObject/MagicHitInfo.cs` | 一次命中的信息：魔法类型、配表数值、命中点 / 法线 / 飞行方向 |
| `MagicType` / `MagicElement` | `GameDataScripts/MagicData/MagicType.cs` | 魔法类型（水/火/岩/气/烤/融）与元素分类枚举（元素**未接入**） |
| `MagicClassBase` + `WaterMagic` 等 6 个子类 | `GameDataScripts/MagicData/MagicClass.cs` | 一条魔法的基础数值（名字/描述/伤害/蓝耗/图标）；子类构造函数从配表拷贝对应类型的数值 |
| `MagicList` | `GameDataScripts/MagicData/MagicList.cs` | ScriptableObject 配表容器（`List<MagicClassBase>`），对外只读 `GetMagicList` |
| `MoveEnum` | `GameDataScripts/MagicData/MoveEnum.cs` | 旧枚举（None/Forward/Rotate），目前没有任何代码使用，与 `MagicMoveType` 概念重叠 |
| `MagicCastTest` | `test/MagicCastTest.cs` | 临时测试脚本：`Start` 里调一次 `Cast`，验完可删 |

## 一、一次释放的完整链路

```text
角色脚本 ──> UseMagic.Cast(MagicType, MagicMoveType)
              │
              ├─ GetCastDirection()      // 释放方向，目前固定返回 Vector3.right（+X）
              ├─ GetSpeed() / GetLifeTime()  // 目前返回 defaultSpeed=40 / defaultLifeTime=2
              │
              └─ Instantiate(MagicObject.prefab, UseMagic 自身位置, LookRotation(方向))
                    │
                    └─ MagicObject.Init(type, moveType, speed, lifeTime)
                          ├─ CreateMagic(type) ──> new WaterMagic(magicList) 等 6 个子类
                          │        └─ 子类构造函数遍历配表，按 GetMagicType 匹配后拷贝数值
                          └─ MagicMove.Init(moveType, speed, lifeTime)
                                   └─ Destroy(gameObject, lifeTime)  // 超时自毁

MagicMove.Update(): 直线 ──> transform.position += transform.forward * speed * Time.deltaTime
```

要点：**配表数值只在实例化那一刻读取一次**；生成之后魔法对象自己持有数值与移动参数，不再访问配表。

## 二、数据结构

| 类型 | 文件 | 职责 |
| --- | --- | --- |
| `MagicType`（enum） | `GameDataScripts/MagicData/MagicType.cs` | 六种魔法类型：水/火/岩/气/烤/融。配表按它匹配，`Cast` 也按它传参 |
| `MagicElement`（enum） | 同上 | 元素分类（水/火/岩/魔力/以太/时间）。**目前未接入**，`magicElement` 字段被注释 |
| `MagicClassBase`（`[System.Serializable]` 纯 C# 类） | `MagicData/MagicClass.cs` | 一条魔法的基础数值，见下表字段 |
| `WaterMagic` … `MeltMagic`（6 个子类） | 同上 | 各自构造函数接收 `MagicList`，把配表里对应类型那条的数值拷进自己 |
| `MagicList`（ScriptableObject） | `MagicData/MagicList.cs` | 配表容器，内部是 `List<MagicClassBase>`，对外只读 `GetMagicList` |
| `MagicMoveType`（enum） | `GameLogic/Player/Magic/MagicMoveType.cs` | 移动方式：直线/追踪/原地（当前只实现直线） |
| `MagicObject`（MonoBehaviour） | `GameLogic/Player/Magic/MagicObject.cs` | 挂在通用魔法预制体上：持有配表引用与本次数值，按类型构造数据类，再把移动参数转交 `MagicMove` |
| `MagicMove`（MonoBehaviour） | `GameLogic/Player/Magic/MagicMove.cs` | 挂在同一个预制体上：按移动方式驱动移动，并负责超时销毁 |
| `UseMagic`（MonoBehaviour） | `GameLogic/Player/Magic/UseMagic.cs` | 挂在角色上，对外唯一入口 `Cast(...)` |

`MagicClassBase` 的数值字段（均为 `protected`，外部通过只读属性读取）：

| 字段 | 只读属性 | 类型 | 说明 |
| --- | --- | --- | --- |
| `magicType` | `GetMagicType` | `MagicType` | 魔法类型，配表按它匹配 |
| `magicName` | `GetMagicName` | `string` | 名字 |
| `description` | `GetDescription` | `string` | 描述 |
| `damage` | `GetDamage` | `int` | 伤害 |
| `manaCost` | `GetManaCost` | `int` | 蓝耗（目前只读出来，未参与消耗逻辑） |
| `magicImage` | `GetMagicImage` | `UnityEngine.UI.Image` | UI 图标 |

预留方法：`virtual public void OnHitSometing()` —— 命中逻辑的重写点，子类重写它即可，目前没有任何调用方。

## 三、配表怎么配（MagicList_SO）

- 资产位置：`Assets/Content/GameData/MagicList_SO.asset`；也可用右键菜单 `Create > GameData > MagicList` 新建。
- 在 Inspector 里展开 `Magic List` 列表，用 `+` 加元素（元素类型就是 `MagicClassBase` 这个普通可序列化类），逐条填写 `magicType`、`magicName`、`description`、`damage`、`manaCost`、`magicImage`。
- 约定：**每种 `MagicType` 只配一条**。子类构造函数是"遍历整张表、类型匹配就拷贝"，且没有 `break`；同类型配了多条时后一条会覆盖前一条。
- 表中缺少某个类型时：`MagicObject` 会打警告，`GetMagicClass` 为 `null`，但魔法对象照常生成与移动。
- 目前这张表是空的（`magicList: []`）。**至少补一条「水」**，否则 `WaterMagic` 构造出来全是默认值。
- 引用关系：`MagicObject.prefab` 上的 `MagicObject.magicList` 指向该资产；换表只改这一个引用。

## 四、怎么构造 / 怎么调用

### 4.1 直接构造数据类（读配表数值）

```csharp
// magicList：Inspector 里拖进来的配表资产
MagicClassBase magic = new WaterMagic(magicList);

string name = magic.GetMagicName;
int damage = magic.GetDamage;
magic.OnHitSometing();   // 命中回调（子类可重写）
```

### 4.2 通过组件释放（正常玩法路径）

```csharp
public class YourCharacter : MonoBehaviour
{
    [SerializeField] private UseMagic useMagic;   // 角色身上的释放组件

    void CastWater()
    {
        useMagic.Cast(MagicType.水, MagicMoveType.直线);
    }
}
```

- 参数一 `MagicType`：决定读配表哪一条、构造哪个子类。
- 参数二 `MagicMoveType`：决定 `MagicMove` 的行为。
- 速度/存活时间目前来自 `UseMagic` 的 `defaultSpeed`(40) 与 `defaultLifeTime`(2 秒)，以后接计算逻辑时改 `GetSpeed()` / `GetLifeTime()` 即可。
- 释放方向由 `GetCastDirection()` 决定，目前固定 `+X`；接角色朝向时只改这一个方法。
- 回到魔法对象上取数值：`magicObject.GetMagicClass.GetDamage`（`GetMagicClass` 是 `MagicObject` 的只读属性）。

### 4.3 场景与预制体需要怎么摆

- 角色物体挂 `UseMagic`，`Magic Prefab` 拖 `Assets/Content/Prefabs/MagicObject.prefab`。
- `MagicObject.prefab`：同物体上必须有 `MagicMove`；`MagicObject` 的 `Magic List` 指向 `MagicList_SO`（缺了只会警告，魔法不会移动）。
- 快速验证：把 `Assets/Content/Scripts/test/MagicCastTest.cs` 挂到场景物体上，`Use Magic` 指向角色的 `UseMagic`，Play 后会释放一次（默认水 + 直线，属临时脚本，可删）。

## 五、扩展：加一种魔法 / 加一种移动方式

- 新魔法类型：`MagicType` 加枚举值 → `MagicClass.cs` 里加一个子类（照抄现有子类，改类型判断）→ `MagicObject.CreateMagic` 加一个 `case` → 配表加一条同类型条目。
- 新移动方式：`MagicMoveType` 加枚举值 → `MagicMove.Update` 加一个分支（`追踪`、`原地` 目前是空分支）。

## 六、当前边界与待办

- 未实现：命中判定、伤害结算、环境交互、特效、冷却、蓝耗扣除；`MagicObject.prefab` 没有渲染体与碰撞体，飞行只体现在 Transform 上。
- `MagicClassBase.OnHitSometing()` 是预留点，拼写未修正，也没有调用方。
- 6 个子类的构造函数里是重复的遍历拷贝代码，可提取到基类（会改基类接口，未做）。
- `MoveEnum.cs`（`None/Forward/Rotate`）目前没有任何代码使用，与 `MagicMoveType` 概念重叠，保留哪个待定。
