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
            self.connected_clients.pop(user_id, None)
        print(f"[WebSocket] 用户 {user_id} 因 HTTP 登出而断开连接")
    
    async def _heartbeat_checker(self):
        """心跳超时检测任务"""
        while True:
            await asyncio.sleep(30)  # 每30秒检查一次
            
            now = time.time()
            timeout_users = []
            
            # 复制一份 key，避免在迭代时发生字典大小改变错误
            for user_id, last_time in list(self._last_heartbeat.items()):
                if now - last_time > HEARTBEAT_TIMEOUT:
                    timeout_users.append(user_id)
            
            for user_id in timeout_users:
                print(f"[WebSocket] 用户 {user_id} 心跳超时，断开连接")
                if user_id in self.connected_clients:
                    websocket = self.connected_clients.pop(user_id, None)
                    if websocket:
                        try:
                            await websocket.close()
                        except:
                            pass
                self._last_heartbeat.pop(user_id, None)
    
    def _update_heartbeat(self, user_id):
        """更新用户心跳时间"""
        if user_id:
            self._last_heartbeat[user_id] = time.time()
    
    async def _send_package(self, websocket, cmd, data_dict):
        """发送自定义协议包: [4字节 CMD] + [4字节 Payload Size] + [Payload]"""
        import struct
        json_str = json.dumps(data_dict, ensure_ascii=False)
        payload = json_str.encode('utf-8')
        header = struct.pack('<ii', cmd, len(payload))
        await websocket.send(header + payload)

    async def handle_client(self, websocket, path):
        """处理客户端连接"""
        user_id = None
        self.all_connections.add(websocket)
        print(f"[WebSocket] 新连接: {websocket.remote_address}, 总连接数: {len(self.all_connections)}")
        
        try:
            async for message in websocket:
                try:
                    # 解析自定义协议包头: [4字节 CMD] + [4字节 Payload Size]
                    if isinstance(message, bytes):
                        if len(message) < 8:
                            print(f"[WebSocket] 消息长度不足: {len(message)}")
                            continue
                        
                        import struct
                        cmd = struct.unpack('<i', message[0:4])[0]
                        payload_size = struct.unpack('<i', message[4:8])[0]
                        payload = message[8:8+payload_size]
                        
                        # 处理心跳 Ping
                        if cmd == 0: # NetworkProtocol.Cmd.Ping
                            print(f"[WebSocket] <Heartbeat> Ping from {websocket.remote_address}")
                            # 返回 Pong: [4字节 CMD=1] + [4字节 Size=0]
                            pong_header = struct.pack('<ii', 1, 0)
                            await websocket.send(pong_header)
                            self._update_heartbeat(user_id)
                            continue
                            
                        # 处理业务 JSON 消息 (假设 CMD=100)
                        if cmd == 100:
                            message_text = payload.decode('utf-8')
                            data = json.loads(message_text)
                        else:
                            # 如果不是已知 CMD，尝试直接解析整个消息为 JSON (兼容旧逻辑)
                            try:
                                data = json.loads(message.decode('utf-8'))
                            except:
                                print(f"[WebSocket] 未知 CMD: {cmd}")
                                continue
                    else:
                        # 文本消息直接解析
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
                        await self._send_package(websocket, 100, response)
                    
                    elif msg_type == 'ping':
                        self._update_heartbeat(user_id)  # 收到ping更新心跳
                        await self._send_package(websocket, 100, {
                            'type': 'pong',
                            'time': time.time()
                        })
                    
                    elif msg_type == 'heartbeat':
                        self._update_heartbeat(user_id)  # 收到heartbeat更新心跳
                        await self._send_package(websocket, 100, {
                            'type': 'heartbeat',
                            'time': time.time()
                        })
                    
                    elif msg_type == 'chat':
                        await self._handle_chat(data, user_id, websocket)
                    
                    elif msg_type == 'player_sync':
                        response = await self._handle_player_sync(data, user_id)
                        await self._send_package(websocket, 100, response)
                    
                    elif msg_type == 'echo':
                        await self._send_package(websocket, 100, {
                            'type': 'echo',
                            'data': data.get('content', '')
                        })
                    
                    else:
                        # 未知消息类型，静默记录
                        print(f"[WebSocket] 未知消息类型: {msg_type}")
                        
                except json.JSONDecodeError:
                    await self._send_package(websocket, 100, {
                        'type': 'error',
                        'msg': 'JSON解析失败'
                    })
                except Exception as e:
                    print(f"[WebSocket] 处理消息错误: {e}")
                    await self._send_package(websocket, 100, {
                        'type': 'error',
                        'msg': str(e)
                    })
                    
        except websockets.exceptions.ConnectionClosed:
            print(f"[WebSocket] 连接关闭: {websocket.remote_address}")
        finally:
            self.all_connections.discard(websocket)
            if user_id:
                self.connected_clients.pop(user_id, None)
                self._last_heartbeat.pop(user_id, None)
                print(f"[WebSocket] 用户下线: {user_id}")
    
    async def _handle_bind_token(self, data, websocket):
        """处理 token 绑定请求（WebSocket 只绑定 HTTP 登录的 token）"""
        token = data.get('token', '')
        
        print(f"[WebSocket] 收到 Token 绑定请求: {token[:8]}...")
        
        if not token:
            print("[WebSocket] 绑定失败: Token 为空")
            return {'type': 'bind_token', 'code': 1, 'msg': 'token不能为空'}
        
        # 使用 HTTP 的 token 验证
        user_id = self.account_manager.validate_token(token)
        if user_id is None:
            print(f"[WebSocket] 绑定失败: Token {token[:8]}... 无效或已过期（可能服务器已重启）")
            return {'type': 'bind_token', 'code': 1, 'msg': 'token无效或已过期'}
        
        # 获取玩家数据
        account_data, accounts_by_name, accounts_by_id = self.account_manager.get_account_data()
        account = accounts_by_id.get(user_id)
        if not account:
            print(f"[WebSocket] 绑定失败: 找不到用户 ID {user_id}")
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
        
        print(f"[WebSocket] Token 绑定成功: 用户={account['username']}({user_id})")
        
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
            await self._send_package(websocket, 100, {
                'type': 'error',
                'msg': '请先登录'
            })
            return
        
        message = {
            'type': 'chat',
            'from': user_id,
            'content': data.get('content', ''),
            'timestamp': time.time()
        }
        
        await self._broadcast(message)
    
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
    
    async def kill_player(self, user_id, reason="被管理员强制下线"):
        """强制断开指定玩家的连接"""
        if user_id in self.connected_clients:
            websocket = self.connected_clients[user_id]
            print(f"[WebSocket] 正在强制下线用户 {user_id}: {reason}")
            try:
                # 先发送一条下线通知给客户端，让客户端能弹出提示
                await self._send_package(websocket, 100, {
                    'type': 'kick',
                    'reason': reason
                })
                # 延迟一小会儿确保消息发出
                await asyncio.sleep(0.1)
                await websocket.close()
                return True
            except Exception as e:
                print(f"[WebSocket] 强制下线用户 {user_id} 失败: {e}")
                return False
        return False

    async def _broadcast(self, message):
        """广播消息给所有已登录客户端"""
        if not self.connected_clients:
            print("[WebSocket] 广播失败: 当前没有已登录的 WebSocket 客户端")
            return
        
        disconnected = []
        
        for uid, client in self.connected_clients.items():
            try:
                # 兼容不同版本的 websockets 库，直接尝试发送，失败则认为连接已断开
                await self._send_package(client, 100, message)
            except Exception as e:
                print(f"[WebSocket] 广播给用户 {uid} 失败: {e}")
                disconnected.append(uid)
        
        # 清理断开的连接
        for uid in disconnected:
            if uid in self.connected_clients:
                del self.connected_clients[uid]
                if uid in self._last_heartbeat:
                    del self._last_heartbeat[uid]
        
        print(f"[WebSocket] 广播完成，成功发送给 {len(self.connected_clients)} 个活跃用户")
    
    async def _broadcast_to_all(self, message):
        """广播消息给所有连接（包括未登录）"""
        if not self.all_connections:
            return
        
        disconnected = []
        
        for client in self.all_connections:
            try:
                await self._send_package(client, 100, message)
            except:
                disconnected.append(client)
        
        # 清理断开的连接
        for client in disconnected:
            self.all_connections.discard(client)
