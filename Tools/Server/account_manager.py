"""
账号管理模块
处理账号数据的加载、保存和验证
"""
import json
import hashlib
import os

ACCOUNT_FILE = os.path.join(os.path.dirname(__file__), 'account.json')


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
        self._online_tokens = {}  # token -> user_id
    
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
        """验证token是否有效"""
        return self._online_tokens.get(token)
    
    def add_token(self, token, user_id):
        """添加在线token"""
        self._online_tokens[token] = user_id
    
    def remove_token(self, token):
        """移除token"""
        if token in self._online_tokens:
            del self._online_tokens[token]
    
    def calculate_level(self, exp):
        """根据经验计算等级，最高100级"""
        exp_table = [
            8, 22, 45, 76, 116, 165, 222, 288, 362, 445,
            537, 637, 746, 864, 990, 1125, 1269, 1421, 1582, 1752,
            1930, 2117, 2313, 2517, 2730, 2952, 3182, 3421, 3669, 3925,
            4190, 4464, 4746, 5037, 5337, 5645, 5962, 6288, 6622, 6965,
            7317, 7677, 8046, 8424, 8810, 9205, 9609, 10021, 10442, 10872,
            11310, 11757, 12213, 12677, 13150, 13632, 14122, 14621, 15129, 15645,
            16170, 16704, 17246, 17797, 18357, 18925, 19502, 20088, 20682, 21285,
            21897, 22517, 23146, 23784, 24430, 25085, 25749, 26421, 27102, 27792,
            28490, 29197, 29913, 30637, 31370, 32112, 32862, 33621, 34389, 35165,
            35950, 36744, 37546, 38357, 39177, 40005, 40842, 41688, 42542, 43405
        ]
        for i in range(len(exp_table) - 1, -1, -1):
            if exp >= exp_table[i]:
                return min(i + 1, 100)  # 最高100级
        return 1
    
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
