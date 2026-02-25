"""
HTTP请求处理器
处理所有HTTP API请求
"""
from http.server import BaseHTTPRequestHandler
import json
import time
from account_manager import AccountManager


class HTTPHandler(BaseHTTPRequestHandler):
    """HTTP请求处理器"""
    
    def __init__(self, *args, **kwargs):
        self.account_manager = AccountManager()
        super().__init__(*args, **kwargs)
    
    def do_POST(self):
        """处理POST请求"""
        account_data, accounts_by_name, accounts_by_id = self.account_manager.get_account_data()
        
        # 注册接口
        if self.path == '/register':
            self._handle_register(accounts_by_name, account_data)
            return

        # 登录接口
        if self.path == '/login':
            self._handle_login(accounts_by_name)
            return

        # 登出接口
        if self.path == '/logout':
            self._handle_logout()
            return
        
        # 资源接口
        if self.path == '/resource/get':
            self._handle_resource_get(accounts_by_id, account_data)
            return
        
        if self.path == '/resource/diamond':
            self._handle_resource_change(accounts_by_id, account_data, 'diamond')
            return
        
        if self.path == '/resource/gold':
            self._handle_resource_change(accounts_by_id, account_data, 'gold')
            return
        
        if self.path == '/resource/exp':
            self._handle_exp_change(accounts_by_id, account_data)
            return
        
        if self.path == '/resource/energy':
            self._handle_resource_change(accounts_by_id, account_data, 'energy')
            return

        # 兑换接口
        if self.path == '/exchange/diamond_to_gold':
            self._handle_exchange_diamond_to_gold(accounts_by_id, account_data)
            return

        # 背包接口
        if self.path == '/inventory/get':
            self._handle_inventory_get(accounts_by_id)
            return
        
        if self.path == '/inventory/add':
            self._handle_inventory_add(accounts_by_id, account_data)
            return
        
        if self.path == '/inventory/remove':
            self._handle_inventory_remove(accounts_by_id, account_data)
            return
        
        if self.path == '/inventory/use':
            self._handle_inventory_use(accounts_by_id, account_data)
            return
        
        # 管理员接口 - 发送公告
        if self.path == '/admin/announce':
            self._handle_announce()
            return
        
        # 404
        self.send_response(404)
        self.end_headers()
    
    def _handle_login(self, accounts_by_name):
        """处理登录请求"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            username = data.get('username', '')
            password = data.get('password', '')
            
            print(f"[HTTP] 登录请求: {username}")
            
            if username not in accounts_by_name:
                response = {'code': 1, 'msg': '账号不存在'}
            else:
                account = accounts_by_name[username]
                if self.account_manager.hash_password(password) != account['password']:
                    response = {'code': 1, 'msg': '密码错误'}
                else:
                    token = self.account_manager.hash_password(f"{username}{time.time()}")
                    self.account_manager.add_token(token, account['userId'])
                    response = {
                        'code': 0,
                        'msg': '登录成功',
                        'token': token,
                        'userId': account['userId'],
                        'username': account['username']
                    }
            
            self._send_json_response(response)

    def _handle_logout(self):
        """处理登出请求"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            token = data.get('token', '')
            
            print(f"[HTTP] 登出请求")
            
            if not token:
                response = {'code': 1, 'msg': 'token不能为空'}
            else:
                user_id = self.account_manager.validate_token(token)
                if user_id is None:
                    response = {'code': 1, 'msg': 'token无效或已过期'}
                else:
                    self.account_manager.remove_token(token)
                    response = {'code': 0, 'msg': '登出成功'}
            
            self._send_json_response(response)

    def _handle_register(self, accounts_by_name, account_data):
        """处理注册请求"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)

            username = data.get('username', '')
            password = data.get('password', '')

            print(f"[HTTP] 注册请求: {username}")

            if not username or not password:
                response = {'code': 1, 'msg': '账号密码不能为空'}
            elif username in accounts_by_name:
                response = {'code': 1, 'msg': '账号已存在'}
            else:
                # 创建新账号
                user_id = self.account_manager.create_account(username, password)
                response = {
                    'code': 0,
                    'msg': '注册成功',
                    'userId': user_id
                }

            self._send_json_response(response)

    def _handle_resource_get(self, accounts_by_id, account_data):
        """获取资源"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)

            token = data.get('token', '')
            user_id = self.account_manager.validate_token(token)

            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    # 计算恢复后的体力
                    energy, max_energy, last_time, has_recovered = self.account_manager.calculate_recovered_energy(account)
                    # 等级根据经验实时计算
                    level = self.account_manager.calculate_level(account.get('exp', 0))

                    response = {
                        'code': 0,
                        'msg': '成功',
                        'diamond': account.get('diamond', 0),
                        'gold': account.get('gold', 0),
                        'exp': account.get('exp', 0),
                        'level': level,
                        'energy': energy,
                        'lastEnergyTime': last_time
                    }

                    # 如果有体力恢复，保存数据
                    if has_recovered:
                        self.account_manager.save_accounts(account_data)

            self._send_json_response(response)
    
    def _handle_resource_change(self, accounts_by_id, account_data, resource_type):
        """处理资源变更"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            token = data.get('token', '')
            amount = data.get('amount', 0)
            
            user_id = self.account_manager.validate_token(token)
            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    # 如果是体力变更，先计算恢复后的值
                    if resource_type == 'energy':
                        current, _, last_time, _ = self.account_manager.calculate_recovered_energy(account)
                        new_amount = current + amount

                        if amount < 0 and new_amount < 0:
                            response = {'code': 1, 'msg': '体力不足'}
                        else:
                            account['energy'] = new_amount
                            # 只在增加体力时更新时间（恢复或道具补充）
                            # 消耗体力时不更新，保持恢复计时
                            if amount > 0:
                                account['lastEnergyTime'] = int(time.time())
                            self.account_manager.save_accounts(account_data)
                            response = {
                                'code': 0,
                                'msg': '成功',
                                'currentEnergy': new_amount
                            }
                    else:
                        current = account.get(resource_type, 0)
                        new_amount = current + amount
                        
                        if amount < 0 and new_amount < 0:
                            response = {'code': 1, 'msg': '资源不足'}
                        else:
                            account[resource_type] = new_amount
                            self.account_manager.save_accounts(account_data)
                            response = {'code': 0, 'msg': '成功', 'currentAmount': new_amount}
            
            self._send_json_response(response)
    
    def _handle_exp_change(self, accounts_by_id, account_data):
        """处理经验变更"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            token = data.get('token', '')
            amount = data.get('amount', 0)
            
            user_id = self.account_manager.validate_token(token)
            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    current = account.get('exp', 0)
                    new_amount = current + amount

                    if amount < 0 and new_amount < 0:
                        response = {'code': 1, 'msg': '经验不足'}
                    else:
                        account['exp'] = new_amount
                        # 等级根据经验实时计算，不存储
                        level = self.account_manager.calculate_level(new_amount)
                        self.account_manager.save_accounts(account_data)
                        response = {'code': 0, 'msg': '成功', 'currentExp': new_amount, 'currentLevel': level}

            self._send_json_response(response)

    def _handle_exchange_diamond_to_gold(self, accounts_by_id, account_data):
        """处理钻石兑换金币"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)

            token = data.get('token', '')
            diamond_amount = data.get('diamondAmount', 0)
            gold_amount = data.get('goldAmount', 0)

            user_id = self.account_manager.validate_token(token)
            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    current_diamond = account.get('diamond', 0)

                    # 检查钻石是否足够
                    if current_diamond < diamond_amount:
                        response = {'code': 1, 'msg': '钻石不足'}
                    else:
                        # 原子操作：扣钻石加金币
                        account['diamond'] = current_diamond - diamond_amount
                        account['gold'] = account.get('gold', 0) + gold_amount
                        self.account_manager.save_accounts(account_data)
                        response = {
                            'code': 0,
                            'msg': '兑换成功',
                            'currentDiamond': account['diamond'],
                            'currentGold': account['gold']
                        }

            self._send_json_response(response)

    def _handle_inventory_get(self, accounts_by_id):
        """获取背包"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            token = data.get('token', '')
            user_id = self.account_manager.validate_token(token)
            
            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    inventory = account.get('inventory', {'items': [], 'maxSlots': 9})
                    response = {'code': 0, 'msg': '成功', 'inventory': inventory}
            
            self._send_json_response(response)
    
    def _handle_inventory_add(self, accounts_by_id, account_data):
        """添加物品"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            token = data.get('token', '')
            item_id = data.get('itemId', '')
            amount = data.get('amount', 1)
            
            user_id = self.account_manager.validate_token(token)
            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    inventory = account.get('inventory', {'items': [], 'maxSlots': 9})
                    items = inventory['items']
                    
                    uid = f"{item_id}_{int(time.time() * 1000)}"
                    items.append({
                        'uid': uid,
                        'itemId': item_id,
                        'count': amount,
                        'bind': False
                    })
                    
                    self.account_manager.save_accounts(account_data)
                    response = {'code': 0, 'msg': '成功', 'inventory': inventory}
            
            self._send_json_response(response)
    
    def _handle_inventory_remove(self, accounts_by_id, account_data):
        """移除物品"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            token = data.get('token', '')
            uid = data.get('uid', '')
            amount = data.get('amount', 1)
            
            user_id = self.account_manager.validate_token(token)
            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    inventory = account.get('inventory', {'items': [], 'maxSlots': 9})
                    items = inventory['items']
                    
                    item = None
                    for i in items:
                        if i['uid'] == uid:
                            item = i
                            break
                    
                    if not item:
                        response = {'code': 1, 'msg': '物品不存在'}
                    elif item['count'] < amount:
                        response = {'code': 1, 'msg': '物品数量不足'}
                    else:
                        item['count'] -= amount
                        if item['count'] <= 0:
                            items.remove(item)
                        self.account_manager.save_accounts(account_data)
                        response = {'code': 0, 'msg': '成功', 'inventory': inventory}
            
            self._send_json_response(response)
    
    def _handle_inventory_use(self, accounts_by_id, account_data):
        """使用物品"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            token = data.get('token', '')
            uid = data.get('uid', '')
            amount = data.get('amount', 1)
            
            user_id = self.account_manager.validate_token(token)
            if not user_id:
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                account = accounts_by_id.get(user_id)
                if not account:
                    response = {'code': 1, 'msg': '玩家不存在'}
                else:
                    inventory = account.get('inventory', {'items': [], 'maxSlots': 9})
                    items = inventory['items']
                    
                    item = None
                    for i in items:
                        if i['uid'] == uid:
                            item = i
                            break
                    
                    if not item:
                        response = {'code': 1, 'msg': '物品不存在'}
                    elif item['count'] < amount:
                        response = {'code': 1, 'msg': '物品数量不足'}
                    else:
                        item['count'] -= amount
                        if item['count'] <= 0:
                            items.remove(item)
                        self.account_manager.save_accounts(account_data)
                        response = {'code': 0, 'msg': '使用成功', 'inventory': inventory}
            
            self._send_json_response(response)
    
    def _handle_announce(self):
        """处理公告请求"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length > 0:
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)
            
            message = data.get('message', '')
            if not message:
                response = {'code': 1, 'msg': '公告内容不能为空'}
            else:
                # 通过WebSocket广播给所有客户端
                if self.websocket_handler:
                    # 创建新的事件循环来运行异步任务
                    import asyncio
                    loop = asyncio.new_event_loop()
                    asyncio.set_event_loop(loop)
                    loop.run_until_complete(
                        self.websocket_handler.broadcast_announcement(message)
                    )
                    loop.close()
                    print(f"[HTTP] 公告已发送: {message}")
                    response = {'code': 0, 'msg': '公告发送成功'}
                else:
                    response = {'code': 1, 'msg': 'WebSocket服务未启动'}
            
            self._send_json_response(response)
    
    def _send_json_response(self, response):
        """发送JSON响应"""
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.send_header('Access-Control-Allow-Origin', '*')
        self.end_headers()
        self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
    
    def do_OPTIONS(self):
        """处理OPTIONS请求（CORS预检）"""
        self.send_response(200)
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.end_headers()
    
    def log_message(self, format, *args):
        """自定义日志"""
        print(f"[HTTP] {args[0]}")
