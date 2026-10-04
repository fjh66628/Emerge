
# Repository Guidelines

Unity 6000.3.25f1 (URP) game-jam project "Emerge". Active development happens on the `Develop` branch.

## Core Principle

**The user is the decision-maker; the agent is the implementer.**

The agent may analyze problems, implement explicitly requested changes, and propose improvements. However, the agent must not make architectural, interface, or design decisions on behalf of the user.

When a change exceeds the user's explicitly requested scope, the agent must stop and ask for approval before implementing it.

## Change Control & Decision Boundaries

- Follow the user's instructions exactly and keep every change within the explicitly requested scope.
- Apply the minimum-change principle: modify only what is necessary to complete the user's request.
- Do not refactor, optimize, rename, reorganize, or clean up unrelated code unless explicitly requested.
- Do not modify unrelated files or systems merely because an improvement is possible.

### User Approval Required

The user retains final decision-making authority over API, interface, and architecture changes.

Before making any of the following changes, stop and ask the user for approval:

- Adding or removing methods.
- Changing method parameters or return types.
- Adding, removing, or renaming classes.
- Changing public/protected interfaces.
- Changing data structures.
- Changing module dependencies.
- Introducing or removing design patterns.
- Changing system or project architecture.
- Performing a large-scale refactor.

When such a change appears necessary:

1. Explain why the change is necessary.
2. Describe the proposed change.
3. Explain which files and systems would be affected.
4. Provide an alternative that avoids the change if possible.
5. Wait for explicit user approval before making the change.

### Suggestions vs. Implementation

- Architectural improvements, refactoring opportunities, and alternative designs may be suggested to the user.
- Suggestions must not be implemented automatically.
- Finding a problem does not authorize fixing it.
- If an unrelated problem is discovered, report it without modifying it.

### Before Editing

Before modifying files:

1. Identify the files and code that need to change.
2. Determine the minimum required modification.
3. Check whether the requested change can be implemented without changing existing interfaces or architecture.
4. If it cannot, ask the user before proceeding.

### After Editing

After completing a task:

- Report the files changed.
- Report the methods/classes changed.
- Report any interfaces added, removed, or modified.
- Report any architectural changes.
- Explicitly state any suggested improvements that were intentionally not implemented.

## Project Structure & Module Organization

- `Assets/Content/Scenes/` — playable scenes. Only `SampleScene.unity` is registered in `ProjectSettings/EditorBuildSettings.asset`; register new scenes there before loading them.
- `Assets/Content/Scripts/GameLogic/` — gameplay code (`GameInstance.cs`, `Singleton.cs`, `EventBus/`, `Manager/`, `Player/`).
- `Assets/Content/Scripts/Presentation/` — view and UI-facing code.
- `Assets/Content/GameData/`, `Assets/Content/Resources/` — runtime data and resources.
- `Assets/Settings/` — URP pipeline assets; touch only deliberately. `Assets/TutorialInfo/` is Unity template content; leave it alone.
- No `.asmdef` exists, so scripts compile into the predefined `Assembly-CSharp`. Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, or `YongXian.slnx`.

## Build, Test, and Development Commands

- Open: Unity Hub → `6000.3.25f1`, or `Unity.exe -projectPath D:\shiyan\unity\gamedevelop\YongXian`.
- Compile: Unity reimports on editor focus; `Ctrl+R` forces a refresh. Check the Console for `error CS…`.
- Tests: `Window > General > Test Runner`; headless via `Unity.exe -runTests -testPlatform PlayMode -projectPath <repo>`.
- Build: `File > Build Profiles`. No CI pipeline exists yet.

## Coding Style & Naming Conventions

- 4-space indent, Allman braces, one top-level type per file, `UnityEngine` first in `using` blocks.
- No namespaces: keep types global, matching existing files.
- `PascalCase` types, methods, and properties; `camelCase` fields and locals (`scriptList`, `event_handlers`).
- Interfaces use the `_interface` suffix: `GameEvent_Interface`, `Instance_interface`.
- Inspector fields are `[SerializeField] private`; avoid public fields.
- Singletons derive from `Singleton<T>` and initialize in `OnSingletonAwake()`. Managers also implement `Instance_interface` (`SwitchOn`/`SwitchOff`).
- Cross-system messaging uses `EventBus.Subscribe<T>` / `Unsubscribe<T>` / `Publish<T>`; payloads implement `GameEvent_Interface`.
- Chinese one-line comments are the norm. No linter, formatter, or `.editorconfig` is configured—match surrounding code.

## Testing Guidelines

No tests exist yet. To add them, create `Assets/Tests/` with its own test assembly definition and move the code under test into a runtime asmdef first, since asmdefs cannot reference `Assembly-CSharp`. Name tests `<Type>Tests` (e.g. `LevelManagerTests`); prefer PlayMode tests for scene and loading behaviour.

## Commit & Pull Request Guidelines

- History is a single `first commit`, so no convention is fixed. Use short imperative subjects scoped by system: `LevelManager: fix additive unload order`.
- Branch off `Develop`; `main` is the release line. Topic branches follow the `mvp04-physical-lighting` style.
- Commit every `.meta` file together with its asset; never move a script without moving its `.meta`.
- PRs target `Develop`, list scene/prefab/serialized-field changes, call out new Build Settings entries, and attach before/after screenshots for visual work.
- `origin` is GitLab (`ssh://git@gitlab.asamalin.top:2222/fjh/emerge.git`); GitHub is a mirror.

## Agent-Specific Instructions

- The Unity Editor is often open and reimporting, so files may appear, disappear, or be renamed mid-task. Re-check the working tree before editing.
- Serialized GUIDs live in `.meta` files; preserve them when adding or moving assets, and report compile warnings you did not fix.\### 环境描述



这是一个游戏mvp测试项目，测试内容：最小玩法，视觉设计



使用unity cli和Unity Editor交互，命令：Usage: unity \[options] \[command]



\### 工程约束



不要进行SHA256

修改完代码之后commit到本地repo

不要使用computer use技能

