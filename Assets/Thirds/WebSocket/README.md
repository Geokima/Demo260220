# WebSocket 长连接模块

基于 Unity 原生 API 的 WebSocket 长连接模块，**无需第三方库**，支持自动重连、心跳检测、消息分发。

## 功能特性

- ✅ **零依赖** - 使用 Unity 原生 `System.Net.WebSockets.ClientWebSocket`
- ✅ **自动重连** - 断线后自动重连，可配置重连次数和间隔
- ✅ **心跳检测** - 定时发送心跳包保持连接
- ✅ **消息分发** - 按消息类型自动分发到对应处理器
- ✅ **线程安全** - 消息队列在主线程处理
- ✅ **WebGL 支持** - 原生支持 WebGL 平台
- ✅ **易于集成** - 提供管理器单例，开箱即用

## 文件结构

```
WebSocket/
├── Runtime/
│   ├── WebSocketClient.cs      # 核心客户端（原生 API）
│   ├── WebSocketManager.cs     # 管理器（单例）
│   └── WebSocketEvents.cs      # 事件定义
└── README.md
```

## 快速开始

### 1. 基本使用

```csharp
using Thirds.WebSocket;

// 获取管理器实例
var ws = WebSocketManager.Instance;

// 连接服务器
ws.Connect("ws://localhost:8080/ws");

// 发送消息
ws.Send("Hello Server");

// 发送 JSON
ws.Send(new { type = "chat", message = "Hi" });
```

### 2. 接收消息

```csharp
// 注册消息处理器
WebSocketManager.Instance.RegisterHandler("chat", OnChatMessage);

void OnChatMessage(string json)
{
    var msg = JsonUtility.FromJson<ChatMessage>(json);
    Debug.Log($"收到消息: {msg.message}");
}
```

### 3. 事件监听

```csharp
var ws = WebSocketManager.Instance.WebSocket;

ws.OnConnected += () => Debug.Log("连接成功");
ws.OnDisconnected += () => Debug.Log("连接断开");
ws.OnError += (error) => Debug.LogError($"错误: {error}");
ws.OnMessageReceived += (msg) => Debug.Log($"收到: {msg}");
```

## 配置参数

在场景中创建空物体并挂载 `WebSocketManager` 脚本：

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Server Url | 服务器地址 | ws://localhost:8080/ws |
| Auto Connect | 启动时自动连接 | false |
| Auto Reconnect | 自动重连 | true |
| Reconnect Interval | 重连间隔(秒) | 3 |
| Max Reconnect Attempts | 最大重连次数 | 5 |
| Enable Heartbeat | 启用心跳 | true |
| Heartbeat Interval | 心跳间隔(秒) | 30 |

## 消息格式

推荐的消息格式（JSON）：

```json
{
  "type": "chat",
  "data": {
    "userId": 1001,
    "message": "Hello"
  }
}
```

根据 `type` 字段自动分发到对应处理器。

## 依赖

- **UniTask**（项目已包含）
- **无其他依赖** - 使用 Unity 原生 WebSocket API

## 平台支持

| 平台 | 支持 |
|------|------|
| Windows | ✅ |
| macOS | ✅ |
| Linux | ✅ |
| Android | ✅ |
| iOS | ✅ |
| WebGL | ✅ |

## 示例场景

```csharp
public class WebSocketExample : MonoBehaviour
{
    void Start()
    {
        var ws = WebSocketManager.Instance;
        
        // 注册处理器
        ws.RegisterHandler("login", OnLoginResponse);
        ws.RegisterHandler("chat", OnChatMessage);
        
        // 连接
        ws.Connect("ws://localhost:8080/ws");
        
        // 监听连接成功
        ws.WebSocket.OnConnected += () =>
        {
            // 发送登录请求
            ws.Send(new { type = "login", token = "xxx" });
        };
    }
    
    void OnLoginResponse(string json)
    {
        Debug.Log("登录响应: " + json);
    }
    
    void OnChatMessage(string json)
    {
        Debug.Log("聊天消息: " + json);
    }
    
    void OnDestroy()
    {
        WebSocketManager.Instance.Disconnect();
    }
}
```

## 注意事项

1. 确保 Unity 的 API Compatibility Level 设置为 .NET Standard 2.1
   - Edit > Project Settings > Player > Api Compatibility Level
2. iOS/Android 平台需要设置允许 HTTP 连接（如果使用 ws://而非 wss://）
   - iOS: Player Settings > Configuration > Allow downloads over HTTP
   - Android: 需要配置 network_security_config

## 许可证

MIT License
