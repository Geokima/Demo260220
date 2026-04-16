# Demo1 项目开发任务列表 (JIRA Style)

## Epic: 基础架构与数据定义 (Core Framework)
| Task ID | Title | Description | Status |
| :--- | :--- | :--- | :--- |
| DEMO1-1 | 建立设计文档 | 完成规则对齐、UI布局方案与ASCII预览 | **DONE** |
| DEMO1-2 | 核心数据模型实现 | 实现 Demo1Model, CardData 及枚举定义 | **DONE** |
| DEMO1-3 | 子架构搭建 | 完成 Demo1Architecture 的 singleton 与模块注册 | **DONE** |

## Epic: 商店与经济系统 (Economy & Shop)
| Task ID | Title | Description | Status |
| :--- | :--- | :--- | :--- |
| DEMO1-4 | 购买逻辑 (BuyCommand) | 实现金币扣除、占位检查、购买触发合并逻辑 | TODO |
| DEMO1-5 | 合并逻辑实现 | 实现 青铜->白银->黄金->钻石 的单跳合并算法 | TODO |
| DEMO1-6 | 商店刷新系统 | 实现 随机随出商品、刷新消耗、离开逻辑 | TODO |

## Epic: 战斗引擎 (Combat Engine)
| Task ID | Title | Description | Status |
| :--- | :--- | :--- | :--- |
| DEMO1-7 | 冷却时间系统 (CD Loop) | 实现战斗中主动卡牌的 CD 轮询与触发逻辑 | TODO |
| DEMO1-8 | 被动效果系统 (Trigger) | 基于标签(Tags)和事件监听的被动卡牌触发机制 | TODO |
| DEMO1-9 | 胜负判断与数值结算 | 处理 HP 归零、声望扣除、EXP 增加、HP重置逻辑 | TODO |

## Epic: 角色成长 (Progression)
| Task ID | Title | Description | Status |
| :--- | :--- | :--- | :--- |
| DEMO1-10 | 等级与扩容系统 | 处理 8 EXP 升级，MaxSlots 动态扩容 (+2) | TODO |
| DEMO1-11 | 回合/天数状态机 | 管理 10天/6回合 的自动转换与轮次逻辑 | TODO |

## Epic: UI 与 表现层 (UI/UX)
| Task ID | Title | Description | Status |
| :--- | :--- | :--- | :--- |
| DEMO1-12 | 主面板(MainPanel)搭建 | 实现 底部HUD + 阵位列表 的基础布局 | TODO |
| DEMO1-13 | 动态上方区域实现 | 根据 UpperMode 动态加载选择/商店/战斗视图 | TODO |
| DEMO1-14 | 备战席(Storage)管理 | 实现 箱子点击弹出、卡牌拖拽、卖出操作 | TODO |
| DEMO1-15 | 战斗动效支撑 | 伤害数字、飞弹投射、CD条视觉反馈 | TODO |
