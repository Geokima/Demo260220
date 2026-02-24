from http.server import HTTPServer, BaseHTTPRequestHandler
import json
import time
import hashlib
import os

ACCOUNT_FILE = os.path.join(os.path.dirname(__file__), 'account.json')

def load_accounts():
    """从文件加载账号数据"""
    with open(ACCOUNT_FILE, 'r', encoding='utf-8') as f:
        return json.load(f)

def save_accounts(data):
    """保存账号数据到文件"""
    with open(ACCOUNT_FILE, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

def get_account_data():
    """获取最新账号数据（每次都重新加载文件）"""
    data = load_accounts()
    accounts_by_name = {acc['username']: acc for acc in data['accounts']}
    accounts_by_id = {acc['userId']: acc for acc in data['accounts']}
    return data, accounts_by_name, accounts_by_id

def hash_password(password):
    """对密码进行SHA256哈希"""
    return hashlib.sha256(password.encode()).hexdigest()

# 在线玩家映射: token -> userId
online_tokens = {}

class LoginHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        # 每次请求重新加载最新数据（支持GM工具实时修改）
        account_data, accounts_by_name, accounts_by_id = get_account_data()
        
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
                    hashed_password = hash_password(password)
                    if account['password'] == hashed_password:
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
        
        # 注册接口
        if self.path == '/register':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                username = data.get('username', '')
                password = data.get('password', '')
                
                print(f"[Server] Register attempt: username={username}")
                
                if not username or not password:
                    response = {'code': 1, 'msg': '账号密码不能为空', 'userId': 0}
                elif username in accounts_by_name:
                    response = {'code': 1, 'msg': '账号已存在', 'userId': 0}
                else:
                    # 创建新账号
                    new_id = max([a['userId'] for a in account_data['accounts']], default=1000) + 1
                    new_account = {
                        'userId': new_id,
                        'username': username,
                        'password': hash_password(password),
                        'diamond': 0,
                        'gold': 0,
                        'exp': 0,
                        'energy': 100,
                        'inventory': {'items': [], 'maxSlots': 9}
                    }
                    account_data['accounts'].append(new_account)
                    accounts_by_id[new_id] = new_account
                    accounts_by_name[username] = new_account
                    save_accounts(account_data)
                    print(f"[Server] Register success: userId={new_id}")
                    response = {'code': 0, 'msg': '注册成功', 'userId': new_id}
            else:
                response = {'code': 1, 'msg': '请求体为空', 'userId': 0}
            
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
                    # 计算离线体力恢复（服务器端计算，安全）
                    current_time = int(time.time())
                    current_energy = account.get('energy', 100)
                    last_energy_time = account.get('lastEnergyTime', current_time)
                    level = account.get('level', 1)
                    max_energy = 100 + (level - 1) * 10
                    
                    # 计算体力恢复
                    if current_energy < max_energy:
                        energy_recover_interval = 10  # 10秒恢复1点
                        elapsed = current_time - last_energy_time
                        if elapsed > 0:
                            recover_points = elapsed // energy_recover_interval
                            if recover_points > 0:
                                new_energy = min(current_energy + recover_points, max_energy)
                                account['energy'] = new_energy
                                print(f"[Server] 离线体力恢复: {current_energy} -> {new_energy} (离线{elapsed}秒)")
                                current_energy = new_energy
                    
                    # 每次获取资源都更新 lastEnergyTime，避免重复计算
                    account['lastEnergyTime'] = current_time
                    save_accounts(account_data)
                    
                    response = {
                        'code': 0,
                        'msg': '成功',
                        'diamond': account.get('diamond', 0),
                        'gold': account.get('gold', 0),
                        'exp': account.get('exp', 0),
                        'level': level,
                        'energy': current_energy,
                        'lastEnergyTime': account.get('lastEnergyTime', current_time)
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
        
        # 经验变更接口
        if self.path == '/resource/exp':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                token = data.get('token', '')
                amount = data.get('amount', 0)
                reason = data.get('reason', '')
                
                print(f"[Server] Exp change request: token={token[:20]}..., amount={amount}, reason={reason}")
                
                user_id = online_tokens.get(token)
                if not user_id:
                    print(f"[Server] Exp change failed: invalid token")
                    response = {'code': 1, 'msg': '未登录或token无效', 'currentExp': 0, 'currentLevel': 1}
                else:
                    print(f"[Server] Exp change: found userId={user_id}")
                    account = accounts_by_id.get(user_id)
                    if not account:
                        print(f"[Server] Exp change failed: account not found for userId={user_id}")
                        response = {'code': 1, 'msg': '玩家不存在', 'currentExp': 0, 'currentLevel': 1}
                    else:
                        current = account.get('exp', 0)
                        new_amount = current + amount
                        
                        if amount < 0 and new_amount < 0:
                            print(f"[Server] Exp change failed: insufficient exp (current={current}, need={-amount})")
                            response = {'code': 1, 'msg': '经验不足', 'currentExp': current, 'currentLevel': account.get('level', 1)}
                        else:
                            account['exp'] = new_amount
                            # 计算等级
                            level = self.calculate_level(new_amount)
                            account['level'] = level
                            save_accounts(account_data)
                            print(f"[Server] Exp change success: {current} -> {new_amount}, level={level}")
                            response = {'code': 0, 'msg': '成功', 'currentExp': new_amount, 'currentLevel': level}
            else:
                print(f"[Server] Exp change failed: empty request body")
                response = {'code': 1, 'msg': '请求体为空', 'currentExp': 0, 'currentLevel': 1}
            
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps(response, ensure_ascii=False).encode())
            return
        
        # 体力变更接口
        if self.path == '/resource/energy':
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length > 0:
                post_data = self.rfile.read(content_length)
                data = json.loads(post_data)
                
                token = data.get('token', '')
                amount = data.get('amount', 0)
                reason = data.get('reason', '')
                
                print(f"[Server] Energy change request: token={token[:20]}..., amount={amount}, reason={reason}")
                
                user_id = online_tokens.get(token)
                if not user_id:
                    print(f"[Server] Energy change failed: invalid token")
                    response = {'code': 1, 'msg': '未登录或token无效', 'currentEnergy': 0, 'maxEnergy': 100}
                else:
                    print(f"[Server] Energy change: found userId={user_id}")
                    account = accounts_by_id.get(user_id)
                    if not account:
                        print(f"[Server] Energy change failed: account not found for userId={user_id}")
                        response = {'code': 1, 'msg': '玩家不存在', 'currentEnergy': 0, 'maxEnergy': 100}
                    else:
                        current = account.get('energy', 100)
                        level = account.get('level', 1)
                        max_energy = 100 + (level - 1) * 10  # 随等级提升
                        new_amount = current + amount
                        
                        if amount < 0 and new_amount < 0:
                            print(f"[Server] Energy change failed: insufficient energy (current={current}, need={-amount})")
                            response = {'code': 1, 'msg': '体力不足', 'currentEnergy': current, 'maxEnergy': max_energy}
                        else:
                            # 检查是否允许超出上限（服务器端判断，安全）
                            # 只有特定原因才允许溢出，如使用道具
                            reason = data.get('reason', '')
                            overflow_reasons = ['使用体力药水', '使用道具', '系统补偿', 'GM命令']
                            allow_overflow = reason in overflow_reasons
                            
                            if allow_overflow:
                                # 道具/奖励可以超出上限
                                account['energy'] = new_amount
                            else:
                                # 自然恢复或普通操作不能超过上限
                                account['energy'] = min(new_amount, max_energy)
                            account['lastEnergyTime'] = int(time.time())
                            save_accounts(account_data)
                            print(f"[Server] Energy change success: {current} -> {account['energy']} (reason={reason}, overflow={allow_overflow})")
                            response = {'code': 0, 'msg': '成功', 'currentEnergy': account['energy'], 'maxEnergy': max_energy}
            else:
                print(f"[Server] Energy change failed: empty request body")
                response = {'code': 1, 'msg': '请求体为空', 'currentEnergy': 0, 'maxEnergy': 100}
            
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
    
    def calculate_level(self, exp):
        """根据经验计算等级"""
        exp_table = [0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500]
        for i in range(len(exp_table) - 1, -1, -1):
            if exp >= exp_table[i]:
                return i + 1
        return 1
    
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
