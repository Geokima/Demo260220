"""
WebSocket处理器
处理所有WebSocket连接和消息
"""
import asyncio
import websockets
import json
import time
from account_manager import AccountManager


HEARTBEAT_TIMEOUT = 120  # 心跳超时时间（秒），2分钟没收到心跳就断开

class WebSocketHandler:
    """WebSocket处理器"""
    
    def __init__(self):
        self.account_manager = AccountManager()
        self.connected_clients = {}  # user_id -> websocket
        self.all_connections = set()  # 所有连接（包括未登录）
        self._last_heartbeat = {}  # user_id -> 最后心跳时间
        # 注册登出回调，HTTP 登出时断开 WebSocket
        self.account_manager.on_logout(self._on_user_logout)
        # 启动心跳超时检测任务
        asyncio.create_task(self._heartbeat_checker())
    
    def _on_user_logout(self, user_id):
        """用户 HTTP 登出时的回调"""
        if user_id in self.connected_clients:
            websocket = self.connected_clients[user_id]
            try:
                # 发送登出通知
                asyncio.create_task(websocket.send(json.dumps({
                    'type': 'force_logout',
                    'msg': '账号已在其他地方登出'
                }, ensure_ascii=False)))
                # 延迟关闭连接，让客户端收到消息
                asyncio.create_task(self._delay_close(websocket, user_id))
            except Exception as e:
                print(f"[WebSocket] 强制登出错误: {e}")
    
    async def _delay_close(self, websocket, user_id, delay=1.0):
        """延迟关闭连接"""
        await asyncio.sleep(delay)
        try:
            await websocket.close()
        except:
            pass
        if user_id in self.connected_clients:
            del self.connected_clients[user_id]
        print(f"[WebSocket] 用户 {user_id} 因 HTTP 登出而断开连接")
    
    async def _heartbeat_checker(self):
        """心跳超时检测任务"""
        while True:
            await asyncio.sleep(30)  # 每30秒检查一次
            
            now = time.time()
            timeout_users = []
            
            for user_id, last_time in self._last_heartbeat.items():
                if now - last_time > HEARTBEAT_TIMEOUT:
                    timeout_users.append(user_id)
            
            for user_id in timeout_users:
                print(f"[WebSocket] 用户 {user_id} 心跳超时，断开连接")
                if user_id in self.connected_clients:
                    websocket = self.connected_clients[user_id]
                    try:
                        await websocket.close()
                    except:
                        pass
                    del self.connected_clients[user_id]
                del self._last_heartbeat[user_id]
    
    def _update_heartbeat(self, user_id):
        """更新用户心跳时间"""
        if user_id:
            self._last_heartbeat[user_id] = time.time()
    
    async def handle_client(self, websocket, path):
        """处理客户端连接"""
        user_id = None
        self.all_connections.add(websocket)
        print(f"[WebSocket] 新连接: {websocket.remote_address}, 总连接数: {len(self.all_connections)}")
        
        try:
            async for message in websocket:
                try:
                    data = json.loads(message)
                    msg_type = data.get('type', '')
                    
                    print(f"[WebSocket] 收到消息: {msg_type}")
                    
                    if msg_type == 'bind_token':
                        # WebSocket 不再处理登录，只绑定已有的 HTTP token
                        response = await self._handle_bind_token(data, websocket)
                        if response.get('code') == 0:
                            user_id = response.get('userId')
                            self.connected_clients[user_id] = websocket
                            self._update_heartbeat(user_id)  # 绑定成功记录心跳
                        await websocket.send(json.dumps(response, ensure_ascii=False))
                    
                    elif msg_type == 'ping':
                        self._update_heartbeat(user_id)  # 收到ping更新心跳
                        await websocket.send(json.dumps({
                            'type': 'pong',
                            'time': time.time()
                        }, ensure_ascii=False))
                    
                    elif msg_type == 'heartbeat':
                        self._update_heartbeat(user_id)  # 收到heartbeat更新心跳
                        await websocket.send(json.dumps({
                            'type': 'heartbeat',
                            'time': time.time()
                        }, ensure_ascii=False))
                    
                    elif msg_type == 'chat':
                        await self._handle_chat(data, user_id, websocket)
                    
                    elif msg_type == 'player_sync':
                        response = await self._handle_player_sync(data, user_id)
                        await websocket.send(json.dumps(response, ensure_ascii=False))
                    
                    elif msg_type == 'echo':
                        await websocket.send(json.dumps({
                            'type': 'echo',
                            'data': data.get('content', '')
                        }, ensure_ascii=False))
                    
                    else:
                        # 未知消息类型，静默记录
                        print(f"[WebSocket] 未知消息类型: {msg_type}")
                        
                except json.JSONDecodeError:
                    await websocket.send(json.dumps({
                        'type': 'error',
                        'msg': 'JSON解析失败'
                    }, ensure_ascii=False))
                except Exception as e:
                    print(f"[WebSocket] 处理消息错误: {e}")
                    await websocket.send(json.dumps({
                        'type': 'error',
                        'msg': str(e)
                    }, ensure_ascii=False))
                    
        except websockets.exceptions.ConnectionClosed:
            print(f"[WebSocket] 连接关闭: {websocket.remote_address}")
        finally:
            self.all_connections.discard(websocket)
            if user_id:
                if user_id in self.connected_clients:
                    del self.connected_clients[user_id]
                if user_id in self._last_heartbeat:
                    del self._last_heartbeat[user_id]
                print(f"[WebSocket] 用户下线: {user_id}")
    
    async def _handle_bind_token(self, data, websocket):
        """处理 token 绑定请求（WebSocket 只绑定 HTTP 登录的 token）"""
        token = data.get('token', '')
        
        print(f"[WebSocket] Token 绑定请求")
        
        if not token:
            return {'type': 'bind_token', 'code': 1, 'msg': 'token不能为空'}
        
        # 使用 HTTP 的 token 验证
        user_id = self.account_manager.validate_token(token)
        if user_id is None:
            return {'type': 'bind_token', 'code': 1, 'msg': 'token无效或已过期'}
        
        # 获取玩家数据
        account_data, accounts_by_name, accounts_by_id = self.account_manager.get_account_data()
        account = accounts_by_id.get(user_id)
        if not account:
            return {'type': 'bind_token', 'code': 1, 'msg': '玩家不存在'}
        
        # 如果该用户已有 WebSocket 连接，先断开旧的
        if user_id in self.connected_clients:
            old_ws = self.connected_clients[user_id]
            try:
                await old_ws.close()
            except:
                pass
            print(f"[WebSocket] 用户 {user_id} 旧连接被挤掉")
        
        # 等级根据经验实时计算
        level = self.account_manager.calculate_level(account.get('exp', 0))
        
        print(f"[WebSocket] Token 绑定成功: user_id={user_id}")
        
        return {
            'type': 'bind_token',
            'code': 0,
            'msg': '绑定成功',
            'userId': user_id,
            'player': {
                'username': account['username'],
                'level': level,
                'exp': account.get('exp', 0),
                'diamond': account.get('diamond', 0),
                'gold': account.get('gold', 0),
                'energy': account.get('energy', 100)
            }
        }
    
    async def _handle_chat(self, data, user_id, websocket):
        """处理聊天消息，广播给所有已登录客户端"""
        if not user_id:
            await websocket.send(json.dumps({
                'type': 'error',
                'msg': '请先登录'
            }, ensure_ascii=False))
            return
        
        message = {
            'type': 'chat',
            'from': user_id,
            'content': data.get('content', ''),
            'timestamp': time.time()
        }
        
        broadcast_msg = json.dumps(message, ensure_ascii=False)
        disconnected = []
        
        for uid, client in self.connected_clients.items():
            try:
                await client.send(broadcast_msg)
            except:
                disconnected.append(uid)
        
        # 清理断开的连接
        for uid in disconnected:
            if uid in self.connected_clients:
                del self.connected_clients[uid]
    
    async def broadcast_announcement(self, message):
        """广播系统公告给已登录用户"""
        import datetime
        announcement = {
            'type': 'announcement',
            'message': message,
            'timestamp': datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')
        }
        await self._broadcast(announcement)
        print(f"[WebSocket] 公告已广播给 {len(self.connected_clients)} 个登录用户: {message}")
    
    async def _handle_player_sync(self, data, user_id):
        """处理玩家数据同步"""
        if not user_id:
            return {'type': 'player_sync', 'code': 1, 'msg': '未登录'}

        account_data, accounts_by_name, accounts_by_id = self.account_manager.get_account_data()

        account = accounts_by_id.get(user_id)
        if not account:
            return {'type': 'player_sync', 'code': 1, 'msg': '玩家不存在'}

        # 等级根据经验实时计算
        level = self.account_manager.calculate_level(account.get('exp', 0))

        return {
            'type': 'player_sync',
            'code': 0,
            'player': {
                'username': account['username'],
                'level': level,
                'exp': account.get('exp', 0),
                'diamond': account.get('diamond', 0),
                'gold': account.get('gold', 0),
                'energy': account.get('energy', 100)
            }
        }
    
    async def _broadcast(self, message):
        """广播消息给所有已登录客户端"""
        if not self.connected_clients:
            return
        
        broadcast_msg = json.dumps(message, ensure_ascii=False)
        disconnected = []
        
        for uid, client in self.connected_clients.items():
            try:
                await client.send(broadcast_msg)
            except:
                disconnected.append(uid)
        
        # 清理断开的连接
        for uid in disconnected:
            if uid in self.connected_clients:
                del self.connected_clients[uid]
    
    async def _broadcast_to_all(self, message):
        """广播消息给所有连接（包括未登录）"""
        if not self.all_connections:
            return
        
        broadcast_msg = json.dumps(message, ensure_ascii=False)
        disconnected = []
        
        for client in self.all_connections:
            try:
                await client.send(broadcast_msg)
            except:
                disconnected.append(client)
        
        # 清理断开的连接
        for client in disconnected:
            self.all_connections.discard(client)
