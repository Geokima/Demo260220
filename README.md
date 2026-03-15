# Unity 工业级架构演示工程

这是一个用于求职展示的 Unity 综合案例，重点展示了**工业级代码分层**、**模块化架构设计 (QFramework 思想)**、**双端全量热更新流**以及**生产力工具链实现**。

## 项目结构概览 (Simplified Tree)

```text
Assets/Game
├── Launch.unity           # 引导底座场景 (YooAsset初始化)
├── GameRes/               # 动态资源树 (受热更管理)
└── _Scripts/              # 逻辑核心
    ├── Main/              # [AOT层] 启动引导、HybridCLR热更驱动
    ├── Framework/         # [内核层] QFramework核心与通用中间件
    │   ├── Common/        # 跨平台核心逻辑 (Core, Utils)
    │   │   ├── Core/      # Architecture, IOC, BindableProperty
    │   │   └── Modules/   # 通用组件 (Config, Http, Res, UI, Pool, Timer)
    │   └── Unity/         # Unity深度集成驱动
    │       └── Editor/    # 工业化工具集 (UI绑定生成、分析器Overlay、引用检查)
    └── Hotfix/            # [热更层] 核心业务逻辑实现 (C# DLL)
        ├── App/           # 全局架构驱动 (GameArchitecture, GameManager)
        ├── Procedures/    # 流程控制 FSM (Launch, Preload, Login, Main)
        ├── Shared/        # 业务共享定义 (Configs, DTOs, Consts, Utils)
        ├── Gateways/      # [通讯网关层] IServerGateway 抽象
        │   ├── LocalServerGateway.cs    # 离线模拟服务器驱动
        │   ├── Controllers/             # [本地服务端逻辑] 模拟后端控制器
        │   └── NetworkServerGateway.cs  # 真实网络服务器 (HTTP/WebSocket)
        ├── Modules/       # 典型业务模块 (统一五层架构)
        │   ├── Inventory/ # 背包系统 (增量同步/响应式字典)
        │   ├── Shop/      # 商店系统 (限购逻辑/服务端同步)
        │   └── ...        # 其他系统模块
        └── Gameplay/      # 核心玩法 space (独立子架构隔离)
            ├── CardBattle/ # 卡牌对战 (Action队列/数值管线)
            └── Match3/     # 三消算法 (逻辑剥离驱动)

Tools/                     # 外部工具链
└── ExcelExporter/         # 基于NPOI的自动化导表流水线
```

---

## 架构职责详述 (Core Responsibilities)

### [Assets/Game] 启动环境
*   **Launch.unity**：框架点火场景，负责初始化 YooAsset 环境并引导后续流程。
*   **GameRes**：分模块管理的动态资源仓库，支持资源热更与按需下载。

### [_Scripts] 逻辑大脑区

#### Main (不动底座)
*   **职责**：负责热更 DLL 的加载注入以及 AOT 元数据的补充，是整个热更机制的起点。

#### Framework (通用工具箱)
##### Common (核心架构)
*   **Core**：提供基于 QFramework 思想的基类及响应式数据绑定核心，实现数据驱动 UI。
*   **Modules (标准化组件)**：
    1. **UI 系统**：基于 UIPanel 与层级管理器的窗体控制系统。
    2. **资源系统 (Res)**：对接 YooAsset 的异步加载与生命周期管理。
    3. **流程系统 (Procedure/FSM)**：控制游戏冷启动到主循环的逻辑切换。
    4. **网络系统 (Network/Http)**：支持 UniTask 驱动的 HTTP 与 WebSocket。
    5. **配置系统 (Config)**：JSON 静态数据的集中加载与解析中心。
    6. **对象池 (Pool)**：针对 GameObject 与内存对象的通用复用池。
    7. **计时器 (Timer)**：支持高精度延时触发与循环任务。
##### Unity/Editor (自研工业化插件)
1. **UI 绑定生成器**：一键将 Prefab 节点映射为 C# 变量，省去手写引用。
2. **UI 性能分析 Overlay**：在场景视图直接监测 Raycast 命中点与 Overdraw。
3. **资源引用检查器**：自动化扫描项目中的预制体丢失引用或异常。

#### Hotfix (动态业务区)
##### App & Procedures (流程控制)
*   **职责**：利用状态机维护从启动、预加载到登录、主城的游戏全局生命周期。
##### Gateways (通讯网关实现)
1. **IServerGateway**：屏蔽底层协议差异的统一调用入口。
2. **LocalServerGateway**：内含本地数据库与控制器的离线模拟服务器。
3. **NetworkServerGateway**：用于线上环境的生产级别网络请求网关。
##### Modules (业务系统)
每个业务模块均按 Model-Service-Command-Syncer 标准五层结构开发：
1. **Auth (认证)**：处理登录、注册与 Token Session 维护。
2. **Inventory (背包)**：支持增量同步、响应式字典与道具操作。
3. **Shop (商店)**：包含限购逻辑、货币核销与商品刷新策略。
4. **Player (玩家)**：处理体力、等级等基础属性的数据同步。
5. **Mail/Mission (邮件与任务)**：完整的业务逻辑闭环，包含领取附件与进度追踪。
##### Gameplay (玩法空间)
采用独立子架构隔离模式，逻辑与外界完全解耦：
1. **CardBattle (卡牌对战)**：基于 Action 队列与数值修饰管线的独立战局系统。
2. **Match3 (三消算法)**：纯逻辑剥离的三消核心，与物理表现分离。

---

## 生产力工具集 (Industrial Tools)
*   **ExcelExporter**：基于 NPOI 实现的自动化导表流水线，支持 JSON 导出与全量结构验证。

---

## 技术亮点总结
*   **架构解耦**：主干管理业务，子架构管理玩法，结构清晰且易于维护。
*   **离线开发**：内置完备的本地模拟服务端，支持在无后端环境下闭环联调业务逻辑。
*   **工业流水线**：配套成熟的自动化导表与 UI 绑定工具，显著提升研发人效。
