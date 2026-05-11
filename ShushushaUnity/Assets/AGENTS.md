# AGENTS.md

## 项目概览

这是 Shushusha 的 Unity 项目。当前主要玩法和客户端代码位于 `Assets` 目录。

主要技术：
- Unity 6 / C#
- FairyGUI UI
- Cysharp UniTask 异步流程
- `System.Text.Json` 客户端/服务端协议序列化
- 基于 TCP 的网络通信，主要通过 `ShushushaServer.Network` 和 `Dispatcher`

## 目录结构

客户端 Unity 项目位于：

- `C:\Users\shiwa\Documents\MyProjects\works\Shushusha\ShushushaUnity`

后端服务项目位于：

- `C:\Users\shiwa\Documents\MyProjects\works\Shushusha\ShushushaServer\ShushushaServer`
- `Program.cs`：服务端入口。
- `Server.cs`：TCP 服务启动、连接接收等服务端基础逻辑。
- `Room.cs`：单个房间的玩家、准备状态、游戏阶段等房间内逻辑。
- `RoomManager.cs`：房间创建、加入、离开、开始游戏等房间管理逻辑。
- `Protocol.cs`：服务端协议 DTO、`MsgId`、`ResCode` 等定义。
- `Dispatcher.cs`：服务端消息分发逻辑。
- `ComDispatcher.cs`：与客户端对应的 JSON 包序列化和长度前缀 TCP 收发工具。
- `ShushushaServer.csproj`：后端 C# 项目文件。

客户端 Assets 主要结构：

- `Scripts/Shushusha/Managers/Game.cs`：核心游戏单例，负责游戏状态、阶段切换，以及网络消息回调。
- `Scripts/Shushusha/Online/Protocol.cs`：消息 ID、请求/响应 DTO、`ResCode`、玩家数据、游戏阶段枚举。
- `Scripts/Shushusha/Online/Request.cs`：客户端请求封装，返回 UniTask 响应 DTO。
- `Scripts/Shushusha/Online/Dispatcher.cs`：网络消息发送/接收队列，以及服务端消息分发。
- `Scripts/Shushusha/Online/ComDispatcher.cs`：JSON 包序列化、反序列化，以及长度前缀 TCP 收发工具。
- `Scripts/Shushusha/UI/*.cs`：手写 FairyGUI partial 类逻辑。
- `Import/FairyGUI/UI/*.cs`：FairyGUI 自动生成类。除非用户明确要求，否则不要手动修改。
- `Scripts/Utils/*.cs`：通用工具，例如 `MonoSingletonBase` 和 FairyGUI/UniTask 扩展。
- `Resources/UI_fui.bytes`、`Resources/UI_atlas0.png`：FairyGUI 生成资源。
- `Scenes/Game.unity`：当前主场景。

## 编辑规则

- 优先修改 `Scripts/Shushusha` 和 `Scripts/Utils` 下的手写代码。
- 不要手动修改 `Import/FairyGUI/UI` 下的 FairyGUI 生成代码。
- 保持 Unity `.meta` 文件完整。新增 Unity 资源时，要注意对应 `.meta` 文件。
- 保留现有中文标识符和 FairyGUI 生成字段名，不要随意重命名 UI 字段。
- 注意编码问题：部分现有文件在终端中可能显示乱码。避免大范围重写或纯格式化修改，以免破坏中文文本或标识符。
- 修改保持小范围、针对性。工作区里可能有用户未提交改动，不要碰无关文件。

## 代码风格

- 延续现有 C# 风格：4 空格缩进、换行大括号、按周围代码习惯使用 `var`，Unity 方法保持简洁。
- UI 行为应写在 `Scripts/Shushusha/UI` 下对应的 partial 类中。
- FairyGUI 组件初始化通常写在 `partial void Init()`。
- FairyGUI 点击事件按现有写法使用 `onClick.Set(...)`。
- 大厅弹窗提示优先使用 `UI_Lobby` 中已有的 `ShowTip(...)`。
- 可恢复或异常失败路径使用 `Debug.LogWarning` / `Debug.LogError`。
- 不要为了抽象而抽象；只有符合现有模式或能减少真实重复时才新增抽象。

## 异步与网络

- Unity 相关异步代码优先使用 UniTask，不使用 `Task`。
- 按钮触发的异步 UI 流程通常使用 `.Forget()`。
- 网络请求应通过 `Request.*`，再由 `Dispatcher.SendMsg(...)` 发送。
- `Dispatcher.Send(...)` 是直接写入网络流的底层方法，普通逻辑不要直接调用。
- 服务端推送消息在 `Game.Update()` 中通过 `Dispatcher.Distribute()` 分发。
- 修改协议时要同时检查客户端 `Assets/Scripts/Shushusha/Online/Protocol.cs` 和后端 `ShushushaServer/ShushushaServer/Protocol.cs`，保持 `MsgId`、DTO 字段、`ResCode` 一致。
- 修改房间/准备/开始游戏等规则时，优先检查后端 `Room.cs`、`RoomManager.cs`，再同步客户端 UI 提示和状态处理。
- 新增协议消息时，通常需要同步更新：
  - `Protocol.cs` 中的 `MsgId` 和 DTO
  - `Request.cs` 中的请求方法
  - `Request.cs` 中的响应 task 字典
  - `Dispatcher.cs` 中的分发逻辑
  - `Game.cs` 或相关 UI partial 类中的回调逻辑

## UI 注意事项

- FairyGUI 生成类会通过 `m_*` 字段暴露 UI 控件。
- UI 逻辑写到 `Scripts/Shushusha/UI` 中匹配的 partial 类，不写进 `Import/FairyGUI/UI` 的生成类。
- 大厅流程目前由 `UI_Lobby` 处理：创建房间、加入房间、准备、开始游戏、成员列表、弹窗提示。
- 游戏主界面流程目前由 `UI_Main`、`Game.OnGameStart`、`Game.OnChangeStage` 等处理。

## 验证方式

- 如果 Unity Editor 可用，优先用 Unity 6000.4.0f1 打开项目并确认编译。
- 本地多开测试依赖 Unity Multiplayer Play Mode 的玩家标签；`Game.Awake()` 会根据 `CurrentPlayer.Tags` 中的 `Player1` / `Player2` 区分本地玩家，否则默认作为第三个玩家。
- 纯代码修改至少检查 `git diff`，并确认受影响 C# 文件没有明显编译错误。
- 当前仓库没有固定的自动化测试命令。
- 不要运行破坏性 git 命令。保留工作区中无关的用户改动。
