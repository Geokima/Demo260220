"""
账号管理模块
处理账号数据的加载、保存和验证
"""
import json
import hashlib
import os
import time

ACCOUNT_FILE = os.path.join(os.path.dirname(__file__), 'account.json')
TOKEN_EXPIRE_TIME = 7 * 24 * 3600  # token过期时间：7天（秒）


class AccountManager:
    """账号管理器"""
    
    _instance = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        self._initialized = True
        # token -> (user_id, expire_time)
        self._online_tokens = {}
        # user_id -> token (用于单点登录)
        self._user_tokens = {}
        # 登出回调函数列表
        self._logout_callbacks = []
    
    @staticmethod
    def load_accounts():
        """从文件加载账号数据"""
        with open(ACCOUNT_FILE, 'r', encoding='utf-8') as f:
            return json.load(f)
    
    @staticmethod
    def save_accounts(data):
        """保存账号数据到文件"""
        with open(ACCOUNT_FILE, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
    
    @staticmethod
    def hash_password(password):
        """对密码进行SHA256哈希"""
        return hashlib.sha256(password.encode()).hexdigest()
    
    def get_account_data(self):
        """获取账号数据及索引"""
        data = self.load_accounts()
        accounts_by_name = {acc['username']: acc for acc in data['accounts']}
        accounts_by_id = {acc['userId']: acc for acc in data['accounts']}
        return data, accounts_by_name, accounts_by_id
    
    def validate_token(self, token):
        """验证token是否有效，返回user_id或None"""
        if token not in self._online_tokens:
            return None
        
        user_id, expire_time = self._online_tokens[token]
        
        # 检查是否过期
        if time.time() > expire_time:
            self.remove_token(token)
            return None
        
        return user_id
    
    def add_token(self, token, user_id):
        """添加在线token（支持单点登录，新token会踢掉旧token）"""
        # 单点登录：如果该用户已有token，先移除旧token
        if user_id in self._user_tokens:
            old_token = self._user_tokens[user_id]
            if old_token in self._online_tokens:
                del self._online_tokens[old_token]
                print(f"[Account] 用户 {user_id} 被挤下线，旧token失效")
        
        # 设置过期时间
        expire_time = time.time() + TOKEN_EXPIRE_TIME
        self._online_tokens[token] = (user_id, expire_time)
        self._user_tokens[user_id] = token
    
    def remove_token(self, token):
        """移除token（登出）"""
        if token in self._online_tokens:
            user_id, _ = self._online_tokens[token]
            del self._online_tokens[token]
            # 同时清理user_tokens映射
            if user_id in self._user_tokens and self._user_tokens[user_id] == token:
                del self._user_tokens[user_id]
            # 触发登出回调
            for callback in self._logout_callbacks:
                try:
                    callback(user_id)
                except Exception as e:
                    print(f"[Account] 登出回调错误: {e}")
    
    def on_logout(self, callback):
        """注册登出回调函数"""
        self._logout_callbacks.append(callback)
    
    def calculate_level(self, exp):
        """根据经验计算等级，最高100级
        exp_table存储的是升到该等级所需的总经验，使用二分查找
        """
        exp_table = [
            8, 30, 75, 151, 267, 432, 654, 942, 1304, 1749,
            2286, 2923, 3669, 4533, 5523, 6648, 7917, 9338, 10920, 12672,
            14602, 16719, 19032, 21549, 24279, 27231, 30413, 33834, 37503, 41428,
            45618, 50082, 54828, 59865, 65202, 70847, 76809, 83097, 89719, 96684,
            104001, 111678, 119724, 128148, 136958, 146163, 155772, 165793, 176235, 187107,
            198417, 210174, 222387, 235064, 248214, 261846, 275968, 290589, 305718, 321363,
            337533, 354237, 371483, 389280, 407637, 426562, 446064, 466152, 486834, 508119,
            530016, 552533, 575679, 599463, 623893, 648978, 674727, 701148, 728250, 756042,
            784532, 813729, 843642, 874279, 905649, 937761, 970623, 1004244, 1038633, 1073798,
            1109748, 1146492, 1184038, 1222395, 1261572, 1301577, 1342419, 1384107, 1426649, 1470054
        ]
        # 二分查找第一个大于exp的位置
        left, right = 0, len(exp_table) - 1
        while left <= right:
            mid = (left + right) // 2
            if exp_table[mid] <= exp:
                left = mid + 1
            else:
                right = mid - 1
        return min(left + 1, 100)  # 最高100级
    
    def get_max_energy(self, level):
        """根据等级计算最大体力"""
        return 100 + (level - 1) * 10
    
    def create_account(self, username, password):
        """创建新账号"""
        data = self.load_accounts()

        # 生成新userId（取最大+1）
        max_id = max([acc['userId'] for acc in data['accounts']], default=10000)
        new_user_id = max_id + 1

        # 创建新账号
        new_account = {
            'userId': new_user_id,
            'username': username,
            'password': self.hash_password(password),
            'diamond': 0,
            'gold': 1000,
            'exp': 0,
            'energy': 100,
            'lastEnergyTime': int(__import__('time').time()),
            'inventory': []
        }

        data['accounts'].append(new_account)
        self.save_accounts(data)

        print(f"[Account] 创建账号成功: {username} (ID: {new_user_id})")
        return new_user_id

    def calculate_recovered_energy(self, account):
        """计算恢复后的体力值，返回 (当前体力, 最大体力, 最后恢复时间, 是否有恢复)"""
        import time

        current_energy = account.get('energy', 100)
        # 等级根据经验实时计算，不存储
        level = self.calculate_level(account.get('exp', 0))
        max_energy = self.get_max_energy(level)
        last_time = account.get('lastEnergyTime', 0)

        print(f"[EnergyDebug] current={current_energy}, max={max_energy}, last_time={last_time}")

        if current_energy >= max_energy:
            # 体力已满，更新计时器为当前时间（为消耗后重新开始计时做准备）
            current_time = int(time.time())
            account['lastEnergyTime'] = current_time
            print(f"[EnergyDebug] 体力已满，更新计时器: {last_time} -> {current_time}")
            return current_energy, max_energy, current_time, False

        current_time = int(time.time())
        elapsed = current_time - last_time

        print(f"[EnergyDebug] current_time={current_time}, elapsed={elapsed}s")

        # 每10秒恢复1点体力
        ENERGY_RECOVER_INTERVAL = 10
        recover_points = elapsed // ENERGY_RECOVER_INTERVAL

        print(f"[EnergyDebug] recover_points={recover_points}")

        if recover_points > 0:
            new_energy = min(current_energy + recover_points, max_energy)
            # 更新存储的值
            account['energy'] = new_energy
            account['lastEnergyTime'] = current_time
            print(f"[EnergyDebug] 恢复体力: {current_energy} -> {new_energy}")
            return new_energy, max_energy, current_time, True

        print(f"[EnergyDebug] 未达到恢复间隔，不恢复")
        return current_energy, max_energy, last_time, False
