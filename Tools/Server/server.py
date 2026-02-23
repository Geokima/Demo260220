from http.server import HTTPServer, BaseHTTPRequestHandler
import json
import time
import hashlib
import os

ACCOUNT_FILE = os.path.join(os.path.dirname(__file__), 'account.json')

def load_accounts():
    with open(ACCOUNT_FILE, 'r') as f:
        return json.load(f)

def save_accounts(data):
    with open(ACCOUNT_FILE, 'w') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

# 加载账号数据
account_data = load_accounts()
accounts_by_name = {acc['username']: acc for acc in account_data['accounts']}
accounts_by_id = {acc['userId']: acc for acc in account_data['accounts']}

print(f"[Server] Loaded {len(accounts_by_name)} accounts")
for acc in account_data['accounts']:
    print(f"[Server]   - {acc['username']} (ID: {acc['userId']})")

# 在线玩家映射: token -> userId
online_tokens = {}

class LoginHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        # 模拟超时
        if self.path == '/login/timeout':
            time.sleep(10)
            self.send_response(200)
            self.end_headers()
            return
        
        # 模拟连接失败
        if self.path == '/login/error':
            self.send_response(500)
            self.send_header('Content-type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({'code': 500, 'msg': '服务器内部错误'}, ensure_ascii=False).encode())
            return
        
        # 模拟慢响应
        if self.path == '/login/slow':
            time.sleep(3)
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({'code': 0, 'msg': '慢响应成功', 'token': 'slow123', 'userId': 10002}).encode())
            return
        
        # 正常登录
        if self.path == '/login':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                username = data.get('username')
                password = data.get('password')
                
                print(f"[Server] Login attempt: username={username}")
                
                account = accounts_by_name.get(username)
                if account:
                    if account['password'] == password:
                        user_id = account['userId']
                        token = f'token_{user_id}_{int(time.time())}'
                        online_tokens[token] = user_id
                        print(f"[Server] Login success: userId={user_id}, token={token[:20]}...")
                        response = {'code': 0, 'msg': '登录成功', 'token': token, 'userId': user_id}
                    else:
                        print(f"[Server] Login failed: password mismatch")
                        response = {'code': 1, 'msg': '账号或密码错误'}
                else:
                    print(f"[Server] Login failed: account not found")
                    response = {'code': 1, 'msg': '账号或密码错误'}
            else:
                print(f"[Server] Login failed: empty request body")
                response = {'code': 1, 'msg': '请求体为空'}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 钻石变更接口
        if self.path == '/resource/diamond':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                token = data.get('token', '')
                amount = data.get('amount', 0)
                reason = data.get('reason', '')
                
                print(f"[Server] Diamond change request: token={token[:20]}..., amount={amount}, reason={reason}")
                
                user_id = online_tokens.get(token)
                if not user_id:
                    print(f"[Server] Diamond change failed: invalid token")
                    response = {'code': 1, 'msg': '未登录或token无效', 'currentAmount': 0}
                else:
                    print(f"[Server] Diamond change: found userId={user_id}")
                    account = accounts_by_id.get(user_id)
                    if not account:
                        print(f"[Server] Diamond change failed: account not found for userId={user_id}")
                        response = {'code': 1, 'msg': '玩家不存在', 'currentAmount': 0}
                    else:
                        current = account['diamond']
                        new_amount = current + amount
                        
                        if amount < 0 and new_amount < 0:
                            print(f"[Server] Diamond change failed: insufficient balance (current={current}, need={-amount})")
                            response = {'code': 1, 'msg': '钻石不足', 'currentAmount': current}
                        else:
                            account['diamond'] = new_amount
                            save_accounts(account_data)
                            print(f"[Server] Diamond change success: {current} -> {new_amount}")
                            response = {'code': 0, 'msg': '成功', 'currentAmount': new_amount}
            else:
                print(f"[Server] Diamond change failed: empty request body")
                response = {'code': 1, 'msg': '请求体为空', 'currentAmount': 0}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 金币变更接口
        if self.path == '/resource/gold':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                token = data.get('token', '')
                amount = data.get('amount', 0)
                reason = data.get('reason', '')
                
                print(f"[Server] Gold change request: token={token[:20]}..., amount={amount}, reason={reason}")
                
                user_id = online_tokens.get(token)
                if not user_id:
                    print(f"[Server] Gold change failed: invalid token")
                    response = {'code': 1, 'msg': '未登录或token无效', 'currentAmount': 0}
                else:
                    print(f"[Server] Gold change: found userId={user_id}")
                    account = accounts_by_id.get(user_id)
                    if not account:
                        print(f"[Server] Gold change failed: account not found for userId={user_id}")
                        response = {'code': 1, 'msg': '玩家不存在', 'currentAmount': 0}
                    else:
                        current = account['gold']
                        new_amount = current + amount
                        
                        if amount < 0 and new_amount < 0:
                            print(f"[Server] Gold change failed: insufficient balance (current={current}, need={-amount})")
                            response = {'code': 1, 'msg': '金币不足', 'currentAmount': current}
                        else:
                            account['gold'] = new_amount
                            save_accounts(account_data)
                            print(f"[Server] Gold change success: {current} -> {new_amount}")
                            response = {'code': 0, 'msg': '成功', 'currentAmount': new_amount}
            else:
                print(f"[Server] Gold change failed: empty request body")
                response = {'code': 1, 'msg': '请求体为空', 'currentAmount': 0}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 获取资源接口
        if self.path == '/resource/get':
            content_length = int(self.headers.get('Content-Length', 0))
            post_data = self.rfile.read(content_length) if content_length > 0 else b'{}'
            data = json.loads(post_data)
            
            token = data.get('token', '')
            print(f"[Server] Get resources request: token={token[:20]}...")
            
            user_id = online_tokens.get(token)
            
            if not user_id:
                print(f"[Server] Get resources failed: invalid token")
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                print(f"[Server] Get resources: found userId={user_id}")
                account = accounts_by_id.get(user_id)
                if account:
                    response = {
                        'code': 0,
                        'msg': '成功',
                        'diamond': account['diamond'],
                        'gold': account['gold']
                    }
                else:
                    print(f"[Server] Get resources failed: account not found for userId={user_id}")
                    response = {'code': 1, 'msg': '玩家不存在'}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 获取背包接口
        if self.path == '/inventory/get':
            content_length = int(self.headers.get('Content-Length', 0))
            post_data = self.rfile.read(content_length) if content_length > 0 else b'{}'
            data = json.loads(post_data)
            
            token = data.get('token', '')
            print(f"[Server] Get inventory request: token={token[:20]}...")
            
            user_id = online_tokens.get(token)
            
            if not user_id:
                print(f"[Server] Get inventory failed: invalid token")
                response = {'code': 1, 'msg': '未登录或token无效'}
            else:
                print(f"[Server] Get inventory: found userId={user_id}")
                account = accounts_by_id.get(user_id)
                if account:
                    response = {
                        'code': 0,
                        'msg': '成功',
                        'inventory': account.get('inventory', {'items': [], 'maxSlots': 9})
                    }
                else:
                    print(f"[Server] Get inventory failed: account not found for userId={user_id}")
                    response = {'code': 1, 'msg': '玩家不存在'}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 添加物品接口
        if self.path == '/inventory/add':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                token = data.get('token', '')
                item_id = data.get('itemId', 0)
                amount = data.get('amount', 1)
                bind = data.get('bind', False)
                
                print(f"[Server] Add item request: token={token[:20]}..., itemId={item_id}, amount={amount}")
                
                user_id = online_tokens.get(token)
                if not user_id:
                    print(f"[Server] Add item failed: invalid token")
                    response = {'code': 1, 'msg': '未登录或token无效'}
                else:
                    account = accounts_by_id.get(user_id)
                    if not account:
                        response = {'code': 1, 'msg': '玩家不存在'}
                    else:
                        inventory = account.setdefault('inventory', {'items': [], 'maxSlots': 9})
                        items = inventory['items']
                        max_slots = inventory.get('maxSlots', 9)
                        
                        # 获取物品配置
                        item_config = account_data.get('itemConfig', {}).get(str(item_id), {})
                        max_stack = item_config.get('maxStack', 10)
                        
                        # 计算需要多少格子
                        remaining = amount
                        
                        # 先尝试填满已有堆叠
                        for item in items:
                            if item['itemId'] == item_id and item['bind'] == bind and item['count'] < max_stack:
                                can_add = min(max_stack - item['count'], remaining)
                                item['count'] += can_add
                                remaining -= can_add
                                if remaining == 0:
                                    break
                        
                        # 还需要新建格子
                        if remaining > 0:
                            # 计算需要几个新格子
                            new_stacks = (remaining + max_stack - 1) // max_stack
                            
                            # 检查格子是否足够
                            if len(items) + new_stacks > max_slots:
                                print(f"[Server] Add item failed: not enough slots ({len(items)} + {new_stacks} > {max_slots})")
                                response = {'code': 1, 'msg': '背包已满', 'inventory': inventory}
                                self.send_response(200)
                                self.send_header('Content-type', 'application/json')
                                self.send_header('Access-Control-Allow-Origin', '*')
                                self.end_headers()
                                self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
                                return
                            
                            # 分堆创建新物品
                            stack_index = 0
                            while remaining > 0:
                                stack_count = min(max_stack, remaining)
                                new_uid = f"item_{int(time.time() * 1000)}_{stack_index}_{len(items)}"
                                items.append({
                                    'uid': new_uid,
                                    'itemId': item_id,
                                    'count': stack_count,
                                    'bind': bind
                                })
                                remaining -= stack_count
                                stack_index += 1
                        
                        save_accounts(account_data)
                        print(f"[Server] Add item success: itemId={item_id}, count={amount}")
                        response = {'code': 0, 'msg': '成功', 'inventory': inventory}
            else:
                response = {'code': 1, 'msg': '请求体为空'}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 移除物品接口
        if self.path == '/inventory/remove':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                token = data.get('token', '')
                uid = data.get('uid', '')
                amount = data.get('amount', 1)
                
                print(f"[Server] Remove item request: token={token[:20]}..., uid={uid}, amount={amount}")
                
                user_id = online_tokens.get(token)
                if not user_id:
                    print(f"[Server] Remove item failed: invalid token")
                    response = {'code': 1, 'msg': '未登录或token无效'}
                else:
                    account = accounts_by_id.get(user_id)
                    if not account:
                        response = {'code': 1, 'msg': '玩家不存在'}
                    else:
                        inventory = account.get('inventory', {'items': [], 'maxSlots': 9})
                        items = inventory['items']
                        
                        # 查找物品
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
                            save_accounts(account_data)
                            print(f"[Server] Remove item success: uid={uid}, removed={amount}")
                            response = {'code': 0, 'msg': '成功', 'inventory': inventory}
            else:
                response = {'code': 1, 'msg': '请求体为空'}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 使用物品接口
        if self.path == '/inventory/use':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                token = data.get('token', '')
                uid = data.get('uid', '')
                amount = data.get('amount', 1)
                
                print(f"[Server] Use item request: token={token[:20]}..., uid={uid}, amount={amount}")
                
                user_id = online_tokens.get(token)
                if not user_id:
                    print(f"[Server] Use item failed: invalid token")
                    response = {'code': 1, 'msg': '未登录或token无效'}
                else:
                    account = accounts_by_id.get(user_id)
                    if not account:
                        response = {'code': 1, 'msg': '玩家不存在'}
                    else:
                        inventory = account.get('inventory', {'items': [], 'maxSlots': 9})
                        items = inventory['items']
                        
                        # 查找物品
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
                            # 获取物品配置
                            item_config = account_data.get('itemConfig', {}).get(str(item['itemId']), {})
                            item_type = item_config.get('type', 'unknown')
                            
                            # 扣除物品
                            item['count'] -= amount
                            if item['count'] <= 0:
                                items.remove(item)
                            
                            save_accounts(account_data)
                            print(f"[Server] Use item success: uid={uid}, type={item_type}, used={amount}")
                            response = {
                                'code': 0,
                                'msg': '成功',
                                'inventory': inventory,
                                'effect': {'type': item_type, 'itemId': item['itemId']}
                            }
            else:
                response = {'code': 1, 'msg': '请求体为空'}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 404
        print(f"[Server] 404: path={self.path}")
        self.send_response(404)
        self.end_headers()
    
    def do_OPTIONS(self):
        self.send_response(200)
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.end_headers()

    def log_message(self, format, *args):
        print(f"[Server] {args[0]}")

if __name__ == '__main__':
    httpd = HTTPServer(('localhost', 8080), LoginHandler)
    print("=" * 50)
    print("登录服务器启动: http://localhost:8080")
    print("=" * 50)
    print("测试端点:")
    print("  POST /login             - 正常登录 (test/123)")
    print("  POST /login/timeout     - 模拟超时(10s)")
    print("  POST /login/error       - 模拟500错误")
    print("  POST /login/slow        - 模拟慢响应(3s)")
    print("  POST /resource/diamond  - 钻石变更")
    print("  POST /resource/gold     - 金币变更")
    print("  POST /resource/get      - 获取资源")
    print("  POST /inventory/get     - 获取背包")
    print("  POST /inventory/add     - 添加物品")
    print("  POST /inventory/remove  - 移除物品")
    print("  POST /inventory/use     - 使用物品")
    print("=" * 50)
    httpd.serve_forever()
