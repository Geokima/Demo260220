"""
游戏服务器启动入口
同时启动HTTP和WebSocket服务
"""
import asyncio
import websockets
from http.server import HTTPServer
import threading
import time
from http_handler import HTTPHandler
from websocket_handler import WebSocketHandler

# 全局WebSocket处理器实例（用于HTTP接口广播）
websocket_handler = None


def run_http_server(port=8080):
    """运行HTTP服务器"""
    # 等待WebSocket服务启动
    while websocket_handler is None:
        time.sleep(0.1)
    
    # 创建自定义Handler类，传入websocket_handler
    class CustomHTTPHandler(HTTPHandler):
        def __init__(self, *args, **kwargs):
            self.websocket_handler = websocket_handler
            super().__init__(*args, **kwargs)
    
    httpd = HTTPServer(('localhost', port), CustomHTTPHandler)
    print(f"[HTTP] 服务器启动: http://localhost:{port}")
    httpd.serve_forever()


async def run_websocket_server(port=8081):
    """运行WebSocket服务器"""
    global websocket_handler
    websocket_handler = WebSocketHandler()
    
    async def handle(websocket):
        """包装处理函数"""
        await websocket_handler.handle_client(websocket, None)
    
    async with websockets.serve(handle, "localhost", port, ping_interval=None):
        print(f"[WebSocket] 服务器启动: ws://localhost:{port}")
        await asyncio.Future()  # 永久运行


def main():
    """主函数"""
    print("=" * 60)
    print("游戏服务器启动")
    print("=" * 60)
    print("HTTP API:   http://localhost:8080")
    print("WebSocket:  ws://localhost:8081")
    print("=" * 60)
    print("HTTP 接口:")
    print("  POST /login            - 登录")
    print("  POST /resource/get     - 获取资源")
    print("  POST /resource/diamond - 钻石变更")
    print("  POST /resource/gold    - 金币变更")
    print("  POST /resource/exp     - 经验变更")
    print("  POST /resource/energy  - 体力变更")
    print("  POST /inventory/get    - 获取背包")
    print("  POST /inventory/add    - 添加物品")
    print("  POST /inventory/remove - 移除物品")
    print("  POST /inventory/use    - 使用物品")
    print("  POST /admin/announce   - 发送公告")
    print("-" * 60)
    print("WebSocket 消息:")
    print("  login         - 登录")
    print("  heartbeat     - 心跳")
    print("  chat          - 聊天（广播）")
    print("  player_sync   - 玩家同步")
    print("  echo          - 回声测试")
    print("  announcement  - 系统公告")
    print("=" * 60)
    
    # 在后台线程运行HTTP服务器
    http_thread = threading.Thread(target=run_http_server, args=(8080,), daemon=True)
    http_thread.start()
    
    # 在主线程运行WebSocket服务器
    try:
        asyncio.run(run_websocket_server(8081))
    except KeyboardInterrupt:
        print("\n服务器已停止")


if __name__ == '__main__':
    main()
