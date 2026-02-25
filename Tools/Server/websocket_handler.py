"""
WebSocket处理器
处理所有WebSocket连接和消息
"""
import asyncio
import websockets
import json
import time
from account_manager import AccountManager


class WebSocketHandler:
    """WebSocket处理器"""
    
    def __init__(self):
        self.account_manager = AccountManager()
        self.connected_clients = {}  # user_id -> websocket
        self.all_connections = set()  # 所有连接（包括未登录）
    
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
                    
                    if msg_type == 'login':
                        response = await self._handle_login(data, websocket)
                        if response.get('code') == 0:
                            user_id = response.get('userId')
                            self.connected_clients[user_id] = websocket
                        await websocket.send(json.dumps(response, ensure_ascii=False))
                    
                    elif msg_type == 'ping':
                        await websocket.send(json.dumps({
                            'type': 'pong',
                            'time': time.time()
                        }, ensure_ascii=False))
                    
                    elif msg_type == 'heartbeat':
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
            if user_id and user_id in self.connected_clients:
                del self.connected_clients[user_id]
                print(f"[WebSocket] 用户下线: {user_id}")
    
    async def _handle_login(self, data, websocket):
        """处理登录请求"""
        account_data, accounts_by_name, accounts_by_id = self.account_manager.get_account_data()
        
        username = data.get('username', '')
        password = data.get('password', '')
        
        print(f"[WebSocket] 登录请求: {username}")
        
        if username not in accounts_by_name:
            return {'type': 'login', 'code': 1, 'msg': '账号不存在'}
        
        account = accounts_by_name[username]
        if self.account_manager.hash_password(password) != account['password']:
            return {'type': 'login', 'code': 1, 'msg': '密码错误'}

        token = self.account_manager.hash_password(f"{username}{time.time()}")
        self.account_manager.add_token(token, account['userId'])

        # 等级根据经验实时计算
        level = self.account_manager.calculate_level(account.get('exp', 0))

        return {
            'type': 'login',
            'code': 0,
            'msg': '登录成功',
            'token': token,
            'userId': account['userId'],
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
