# 游戏服务器 API 文档

## 基础信息

- **基础URL**: `http://localhost:8080`
- **请求方式**: POST
- **数据格式**: JSON
- **响应格式**: JSON

## 通用响应格式

```json
{
    "code": 0,      // 0=成功, 1=失败
    "msg": "成功",   // 提示信息
    ...其他数据
}
```

---

## 1. 登录接口

### POST /login

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| username | string | 是 | 账号名 |
| password | string | 是 | 密码(明文,服务器会哈希) |

**请求示例:**
```json
{
    "username": "test",
    "password": "123456"
}
```

**成功响应:**
```json
{
    "code": 0,
    "msg": "登录成功",
    "token": "xxx...",
    "userId": 10001,
    "player": {
        "username": "test",
        "level": 5,
        "exp": 1400,
        "diamond": 450,
        "gold": 1000,
        "energy": 160
    }
}
```

**失败响应:**
```json
{
    "code": 1,
    "msg": "账号不存在"
}
```

---

## 2. 资源接口

### 2.1 获取资源

#### POST /resource/get

获取玩家所有资源信息,包含体力恢复计算。

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |

**请求示例:**
```json
{
    "token": "xxx..."
}
```

**成功响应:**
```json
{
    "code": 0,
    "msg": "成功",
    "diamond": 450,
    "gold": 1000,
    "exp": 1400,
    "level": 5,
    "energy": 160,
    "lastEnergyTime": 1771974104
}
```

---

### 2.2 变更钻石

#### POST /resource/diamond

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |
| amount | int | 是 | 变更数量(正数增加,负数减少) |
| reason | string | 否 | 变更原因 |

**请求示例:**
```json
{
    "token": "xxx...",
    "amount": -10,
    "reason": "购买物品"
}
```

**成功响应:**
```json
{
    "code": 0,
    "msg": "成功",
    "currentAmount": 440
}
```

**失败响应:**
```json
{
    "code": 1,
    "msg": "资源不足"
}
```

---

### 2.3 变更金币

#### POST /resource/gold

参数和响应格式同 `/resource/diamond`

---

### 2.4 变更经验

#### POST /resource/exp

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |
| amount | int | 是 | 变更数量 |
| reason | string | 否 | 变更原因 |

**成功响应:**
```json
{
    "code": 0,
    "msg": "成功",
    "currentExp": 1500,
    "currentLevel": 6
}
```

**注意:** 等级根据经验实时计算,不存储在数据库中。

---

### 2.5 变更体力

#### POST /resource/energy

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |
| amount | int | 是 | 变更数量 |
| reason | string | 否 | 变更原因 |

**成功响应:**
```json
{
    "code": 0,
    "msg": "成功",
    "currentEnergy": 150
}
```

**体力恢复机制:**
- 每10秒恢复1点体力
- 消耗体力时不重置计时器
- 体力满时更新计时器为当前时间

---

## 3. 兑换接口

### POST /exchange/diamond_to_gold

钻石兑换金币,原子操作。

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |
| diamondAmount | int | 是 | 消耗钻石数量 |
| goldAmount | int | 是 | 获得金币数量 |

**请求示例:**
```json
{
    "token": "xxx...",
    "diamondAmount": 1,
    "goldAmount": 100
}
```

**成功响应:**
```json
{
    "code": 0,
    "msg": "兑换成功",
    "currentDiamond": 449,
    "currentGold": 1100
}
```

**失败响应:**
```json
{
    "code": 1,
    "msg": "钻石不足"
}
```

---

## 4. 背包接口

### 4.1 获取背包

#### POST /inventory/get

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |

**成功响应:**
```json
{
    "code": 0,
    "msg": "成功",
    "inventory": {
        "items": [
            {
                "uid": "item_1234567890",
                "itemId": 1001,
                "count": 5,
                "bind": false
            }
        ],
        "maxSlots": 9
    }
}
```

---

### 4.2 添加物品

#### POST /inventory/add

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |
| itemId | string/int | 是 | 物品ID |
| amount | int | 否 | 数量(默认1) |

**请求示例:**
```json
{
    "token": "xxx...",
    "itemId": 1001,
    "amount": 1
}
```

---

### 4.3 移除物品

#### POST /inventory/remove

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |
| uid | string | 是 | 物品唯一ID |
| amount | int | 否 | 数量(默认1) |

**请求示例:**
```json
{
    "token": "xxx...",
    "uid": "item_1234567890",
    "amount": 1
}
```

---

### 4.4 使用物品

#### POST /inventory/use

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| token | string | 是 | 登录令牌 |
| uid | string | 是 | 物品唯一ID |
| amount | int | 否 | 数量(默认1) |

---

## 5. 管理员接口

### POST /admin/announce

发送全服公告(通过WebSocket广播)。

**请求参数:**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| message | string | 是 | 公告内容 |

**请求示例:**
```json
{
    "message": "服务器维护通知"
}
```

**成功响应:**
```json
{
    "code": 0,
    "msg": "公告发送成功"
}
```

---

## 游戏机制说明

### 等级系统
- 等级根据经验实时计算,不存储
- 经验需求表: 1级8经验, 100级43405经验
- 最高100级

### 体力系统
- 基础体力: 100点
- 每级增加: 10点上限
- 恢复速度: 10秒/点
- 消耗体力不重置恢复计时

### 资源验证
- 所有资源变更都有服务端验证
- 钻石/金币/经验不足时会返回错误
- 体力变更会先计算恢复后的值
