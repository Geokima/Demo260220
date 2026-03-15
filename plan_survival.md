# 《生存小游戏》模块详细工程实施计划 (Master Implementation Specification)

> [!IMPORTANT]
> **设计指导思想**：
> - **微内核设计**：生存模块作为独立插件，通过 `SurvivalArchitecture` 彻底隔离局内数据。
> - **确定性逻辑**：所有属性变化由 `SurvivalService` 统一管理，UI 仅做展现。
> - **商业级闭环**：生存结果（Session）通过安全审计转换为全局账号经验（XP）。

---

## 1. 目录结构与类职责映射 (File Map)

```text
Assets/Game/_Scripts/Hotfix/Gameplay/Survival/
├── SurvivalArchitecture.cs        # [Core] 生命周期管理与模块注册中心
├── SurvivalModel.cs                # [Data] 局内响应式数据中心 (Health, Hunger, Sanity, Days)
├── SurvivalService.cs              # [Service] 逻辑心跳 (Tick) 调度与生存规则引擎
├── Numerical/
│   └── SurvivalNumerical.cs        # [Logic] 纯算法类：衰减公式、伤害修正
├── Actions/
│   ├── EatAction.cs                # [Action] 进食指令与属性恢复
│   ├── HarvestAction.cs            # [Action] 资源采集逻辑
│   └── InstantDeathAction.cs       # [Action] 黑暗致死或强制处决
├── Inventory/
│   ├── WorldInventoryModel.cs      # [Data] 独立 Slot 管理，与全局账号仓库隔离
│   ├── WorldInventorySyncer.cs    # [Syncer] 处理局内背包变更对齐
│   └── Components/
│       └── InventoryComponent.cs   # [Component] 可挂载实体组件：支持玩家、切斯特、箱子
├── Time/
│   └── WorldClockService.cs        # [Service] 时钟推进、昼夜相位切换 (Day/Dusk/Night)
├── Settlement/
│   ├── SettlementService.cs        # [Service] 经验结算上报、防挂行为审计
│   └── SettlementEvents.cs          # [Event] 死亡与结算完成的广播事件
└── DTOs/
    ├── SurvivalSessionDto.cs       # [DTO] 局内序列化快照
    └── SettlementRequestDto.cs     # [DTO] 结算上报包
```

---

## 2. 核心系统详细设计 (Technical Design)

### 2.1 生存三维与 Tick 循环 (Survival Stats & Heartbeat)
- **实现方案**：在 `SurvivalService` 中开启一个 `UniTask.Repeat` 循环 (1s/Tick)。
- **逻辑流**：
  1. `Hunger.Value -= 1` (从 Config 读取衰减率)。
  2. 若 `Hunger.Value <= 0`，向 `ActionQueue` 推送 `ConstantDamageAction`。
  3. 判定当前亮度：若在黑夜且无光源实体，触发黑暗警告 → 5s 后执行 `InstantDeathAction`。
- **UI 对齐**：`HUDView` 通过 `Subscribe` 监听 `Hunger` 变动，仅在主线程刷新 UI。

### 2.2 世界库存与容器模型 (World Inventory & Decoration)
- **核心逻辑**：所有实体均可挂载 `InventoryComponent`。
- **隔离方案**：`WorldInventoryModel` 不继承 `InventoryModel`。它使用独立的 Uid 索引（局内 Uid），确保不会误操作到外界皮肤仓库。
- **存储对齐**：
  - **玩家**：Model 默认持有 15 个 Slot。
  - **切斯特/箱子**：通过 `GetComponent<InventoryComponent>()` 实时获取数据包。

### 2.3 动作同步与授权 (Action Pipeline)
- **指令式交互**：
  - 玩家点击树木 → `SendCommand(new InteractCommand { Target = tree, Action = Chop })`。
  - `SurvivalService` 校验距离和工具状态。
  - 校验通过 → 调用 `ActionQueue.Enqueue()` 播放动画并扣除耐久。
- **即时反馈**：工具损坏事件由 Model 广播，UI 直接弹出“工具已损坏”提示并移除图标。

### 2.4 死亡结算与审计 (Settlement & Security)
- **审计记录 (Audit Ledger)**：`SurvivalModel` 在后台通过 `List<ActionRecord>` 记录：
  - `[TIMESTAMP] EAT_FOOD_ID_101`
  - `[TIMESTAMP] KILL_MONSTER_ID_202`
- **结算协议**：
  - 死亡时，`SettlementService` 构造一个 `SettlementRequest`。
  - **由后端（或本地加密验证类）** 对比 `ActionRecord` 的总数与生存时间是否匹配。
  - 校验成功：向全局 `PlayerModel.Exp` 注入增量。

---

## 3. 详细开发阶段与 Task 拆解 (Phases)

### 阶段一：模块底包与静态数据 (Phases 1)
- [ ] **Task 1.1**: 创建 `SurvivalArchitecture`，完成 `Model/Service` 的基础注册。
- [ ] **Task 1.2**: 配置 `cfg_survival_items.json`，定义基础道具：斧头、木头、草、浆果、肉。
- [ ] **Task 1.3**: 实现 `SurvivalModel` 的三维响应式属性（Health/Hunger/Sanity）。

### 阶段二：生存心跳与生理系统 (Phases 2)
- [ ] **Task 2.1**: 在 `SurvivalService` 中实现 1s/次 的逻辑 Tick。
- [ ] **Task 2.2**: 实现“饿死判定”逻辑管线。
- [ ] **Task 2.3**: 接入 `WorldClockService`，实现基于时间的速度缩放（白天/黄昏/黑夜）。

### 阶段三：物体交互与背包闭环 (Phases 3)
- [ ] **Task 3.1**: 开发 `WorldInventoryModel`，支持 Slot 交换。
- [ ] **Task 3.2**: 开发 `CollectAction`，实现点击实体 -> 扣除耐久 -> 背包增项。
- [ ] **Task 3.3**: 实现“科学机器”靠近检测，触发 UI 配方解锁。

### 阶段四：死亡结算与 XP 转化 (Phases 4)
- [ ] **Task 4.1**: 实现 `InstantDeathAction` 黑暗致死逻辑。
- [ ] **Task 4.2**: 开发 `SettlementService`，建立与 `IServerGateway` 的结算指令对接。
- [ ] **Task 4.3**: 验证死亡后账号经验增加的流程。

---

## 4. 关键 DTO 协议预览 (API Protocol)

```csharp
// 结算请求
public class SettlementRequest
{
    public long SessionId;     // 当前局唯一标识
    public int LiveDays;       // 生存天数
    public int TotalScore;     // 综合评分
    public string AuditHash;   // 行为日志的加密摘要，用于防篡改
}

// 局内实体快照
public class EntitySnapshotDto
{
    public int EntityType;     // 实体配置 ID
    public float CurHealth;    // 剩余耐久或生命
    public List<int> ItemIds;  // 如果是容器，存储物品列表
}
```

---

## 5. 验收标准
- [ ] 挂机 100 秒，玩家会因为饥饿降至 0 且生命值扣完而“自然死亡”。
- [ ] 死亡后，主界面的账号经验条有对应数值提升。
- [ ] 在黑夜中熄灭火把，5 秒内角色必须死亡。
- [ ] 点击箱子能将背包装载物成功存入并持久化（在该 Session 内）。
