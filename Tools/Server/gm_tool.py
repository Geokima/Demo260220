#!/usr/bin/env python3
"""
GM 管理工具 - 用于后台管理玩家数据
用法: python gm_tool.py <命令> [参数]

命令大全:
  查询类:
    list                          列出所有玩家账号
    query <username>              查询指定玩家详细信息（资源、背包物品等）

  资源类:
    give_diamond <username> <amount>      给玩家发放钻石
    give_gold <username> <amount>         给玩家发放金币
    give_exp <username> <amount>          给玩家发放经验
    give_energy <username> <amount>       给玩家发放体力

  物品类:
    give_item <username> <item_id> <amount> [--bind]    给玩家发放物品
                                          --bind 表示绑定物品
    remove_item <username> <uid>          删除玩家指定物品（uid从query获取）

  账号类:
    set_password <username> <password>    修改玩家密码

  公告类:
    announce <message>                    发送全服公告

示例:
  python gm_tool.py list
  python gm_tool.py query test
  python gm_tool.py give_diamond test 1000
  python gm_tool.py give_gold test 5000
  python gm_tool.py give_exp test 500
  python gm_tool.py give_energy test 50
  python gm_tool.py give_item test 1001 5
  python gm_tool.py give_item test 1001 5 --bind
  python gm_tool.py remove_item test item_123456
  python gm_tool.py set_password test 123456
  python gm_tool.py announce "服务器将在10分钟后维护"
"""

import json
import os
import sys
import argparse
from datetime import datetime

ACCOUNT_FILE = os.path.join(os.path.dirname(__file__), 'account.json')

def load_accounts():
    with open(ACCOUNT_FILE, 'r', encoding='utf-8') as f:
        return json.load(f)

def save_accounts(data):
    with open(ACCOUNT_FILE, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

def find_account(data, username):
    for acc in data['accounts']:
        if acc['username'] == username:
            return acc
    return None

def calculate_level(exp):
    """根据经验计算等级"""
    exp_table = [0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500]
    for i in range(len(exp_table) - 1, -1, -1):
        if exp >= exp_table[i]:
            return i + 1
    return 1

def cmd_list(args):
    """列出所有玩家"""
    data = load_accounts()
    print(f"{'UserID':<8} {'Username':<15} {'Level':<6} {'Exp':<8} {'Energy':<8} {'Diamond':<10} {'Gold':<10}")
    print("-" * 75)
    for acc in data['accounts']:
        exp = acc.get('exp', 0)
        level = calculate_level(exp)
        energy = acc.get('energy', 100)
        print(f"{acc['userId']:<8} {acc['username']:<15} {level:<6} {exp:<8} {energy:<8} {acc.get('diamond', 0):<10} {acc.get('gold', 0):<10}")

def cmd_query(args):
    """查询玩家详情"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    exp = acc.get('exp', 0)
    level = calculate_level(exp)
    max_energy = 100 + (level - 1) * 10
    
    print(f"玩家信息: {acc['username']}")
    print(f"  UserID: {acc['userId']}")
    print(f"  等级: {level}")
    print(f"  经验: {exp}")
    print(f"  体力: {acc.get('energy', 100)} / {max_energy}")
    print(f"  钻石: {acc.get('diamond', 0)}")
    print(f"  金币: {acc.get('gold', 0)}")
    print(f"  背包格子: {acc.get('inventory', {}).get('maxSlots', 9)}")
    print(f"  物品数量: {len(acc.get('inventory', {}).get('items', []))}")
    
    items = acc.get('inventory', {}).get('items', [])
    if items:
        print("  物品列表:")
        for item in items:
            print(f"    - {item['uid']}: itemId={item['itemId']}, count={item['count']}, bind={item['bind']}")

def cmd_give_diamond(args):
    """给玩家发钻石"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    old = acc.get('diamond', 0)
    acc['diamond'] = old + args.amount
    save_accounts(data)
    
    print(f"成功: {args.username} 钻石 {old} -> {acc['diamond']} (+{args.amount})")

def cmd_give_gold(args):
    """给玩家发金币"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    old = acc.get('gold', 0)
    acc['gold'] = old + args.amount
    save_accounts(data)
    
    print(f"成功: {args.username} 金币 {old} -> {acc['gold']} (+{args.amount})")

def cmd_give_exp(args):
    """给玩家发经验"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    old = acc.get('exp', 0)
    old_level = calculate_level(old)
    acc['exp'] = old + args.amount
    new_level = calculate_level(acc['exp'])
    save_accounts(data)
    
    print(f"成功: {args.username} 经验 {old} -> {acc['exp']} (+{args.amount})")
    if new_level != old_level:
        print(f"  升级: {old_level}级 -> {new_level}级")

def cmd_give_energy(args):
    """给玩家发体力"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    old = acc.get('energy', 100)
    level = calculate_level(acc.get('exp', 0))
    max_energy = 100 + (level - 1) * 10
    acc['energy'] = min(old + args.amount, max_energy)
    save_accounts(data)
    
    print(f"成功: {args.username} 体力 {old} -> {acc['energy']} (+{args.amount}, 上限{max_energy})")

def cmd_give_item(args):
    """给玩家发物品"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    inventory = acc.setdefault('inventory', {'items': [], 'maxSlots': 9})
    items = inventory['items']
    
    # 生成唯一ID
    uid = f"gm_{int(datetime.now().timestamp() * 1000)}_{len(items)}"
    new_item = {
        'uid': uid,
        'itemId': args.item_id,
        'count': args.amount,
        'bind': args.bind
    }
    items.append(new_item)
    save_accounts(data)
    
    print(f"成功: {args.username} 获得物品 itemId={args.item_id} x{args.amount} (bind={args.bind})")

def cmd_remove_item(args):
    """删除玩家物品"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    inventory = acc.get('inventory', {'items': []})
    items = inventory['items']
    
    for i, item in enumerate(items):
        if item['uid'] == args.uid:
            removed = items.pop(i)
            save_accounts(data)
            print(f"成功: 删除物品 {removed['uid']} (itemId={removed['itemId']}, count={removed['count']})")
            return
    
    print(f"错误: 物品 UID '{args.uid}' 不存在")

def cmd_set_password(args):
    """修改玩家密码"""
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    acc['password'] = args.password
    save_accounts(data)
    print(f"成功: {args.username} 密码已修改")

def cmd_announce(args):
    """发送全服公告（通过HTTP接口）"""
    import urllib.request
    import urllib.error
    
    url = 'http://localhost:8080/admin/announce'
    data = json.dumps({'message': args.message}, ensure_ascii=False).encode('utf-8')
    
    try:
        req = urllib.request.Request(
            url,
            data=data,
            headers={'Content-Type': 'application/json'},
            method='POST'
        )
        
        with urllib.request.urlopen(req) as response:
            result = json.loads(response.read().decode('utf-8'))
            if result.get('code') == 0:
                print(f"成功: 公告已发送")
                print(f"  内容: {args.message}")
            else:
                print(f"失败: {result.get('msg', '未知错误')}")
    except urllib.error.URLError as e:
        print(f"错误: 无法连接到服务器 - {e}")
    except Exception as e:
        print(f"错误: {e}")

def cmd_kick(args):
    """强制玩家下线（通过HTTP接口）"""
    import urllib.request
    import urllib.error
    
    # 1. 先通过用户名查到 userId
    data = load_accounts()
    acc = find_account(data, args.username)
    if not acc:
        print(f"错误: 玩家 '{args.username}' 不存在")
        return
    
    user_id = acc.get('userId')
    
    # 2. 发送 Kick 请求
    url = 'http://localhost:8080/admin/kick'
    payload = json.dumps({
        'userId': user_id,
        'reason': args.reason
    }, ensure_ascii=False).encode('utf-8')
    
    try:
        req = urllib.request.Request(
            url,
            data=payload,
            headers={'Content-Type': 'application/json'},
            method='POST'
        )
        
        with urllib.request.urlopen(req) as response:
            result = json.loads(response.read().decode('utf-8'))
            if result.get('code') == 0:
                print(f"成功: 用户 {args.username}({user_id}) 已被强制下线")
                print(f"  原因: {args.reason}")
            else:
                print(f"失败: {result.get('msg', '未知错误')}")
                
    except Exception as e:
        print(f"错误: 无法连接到服务器 - {e}")

def main():
    parser = argparse.ArgumentParser(description='GM 管理工具')
    subparsers = parser.add_subparsers(dest='command', help='可用命令')
    
    # list
    subparsers.add_parser('list', help='列出所有玩家')
    
    # query
    p_query = subparsers.add_parser('query', help='查询玩家详情')
    p_query.add_argument('username', help='玩家账号')
    
    # give_diamond
    p_diamond = subparsers.add_parser('give_diamond', help='给玩家发钻石')
    p_diamond.add_argument('username', help='玩家账号')
    p_diamond.add_argument('amount', type=int, help='数量')
    
    # give_gold
    p_gold = subparsers.add_parser('give_gold', help='给玩家发金币')
    p_gold.add_argument('username', help='玩家账号')
    p_gold.add_argument('amount', type=int, help='数量')
    
    # give_exp
    p_exp = subparsers.add_parser('give_exp', help='给玩家发经验')
    p_exp.add_argument('username', help='玩家账号')
    p_exp.add_argument('amount', type=int, help='数量')
    
    # give_energy
    p_energy = subparsers.add_parser('give_energy', help='给玩家发体力')
    p_energy.add_argument('username', help='玩家账号')
    p_energy.add_argument('amount', type=int, help='数量')
    
    # give_item
    p_item = subparsers.add_parser('give_item', help='给玩家发物品')
    p_item.add_argument('username', help='玩家账号')
    p_item.add_argument('item_id', type=int, help='物品配置ID')
    p_item.add_argument('amount', type=int, help='数量')
    p_item.add_argument('--bind', action='store_true', help='是否绑定')
    
    # remove_item
    p_remove = subparsers.add_parser('remove_item', help='删除玩家物品')
    p_remove.add_argument('username', help='玩家账号')
    p_remove.add_argument('uid', help='物品唯一ID')
    
    # set_password
    p_pass = subparsers.add_parser('set_password', help='修改玩家密码')
    p_pass.add_argument('username', help='玩家账号')
    p_pass.add_argument('password', help='新密码')
    
    # announce
    p_announce = subparsers.add_parser('announce', help='发送全服公告')
    p_announce.add_argument('message', help='公告内容')
    
    # kick
    p_kick = subparsers.add_parser('kick', help='强制玩家下线')
    p_kick.add_argument('username', help='玩家账号')
    p_kick.add_argument('--reason', default='被管理员强制下线', help='下线原因')
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        return
    
    # 执行命令
    commands = {
        'list': cmd_list,
        'query': cmd_query,
        'give_diamond': cmd_give_diamond,
        'give_gold': cmd_give_gold,
        'give_exp': cmd_give_exp,
        'give_energy': cmd_give_energy,
        'give_item': cmd_give_item,
        'remove_item': cmd_remove_item,
        'set_password': cmd_set_password,
        'announce': cmd_announce,
        'kick': cmd_kick,
    }
    
    func = commands.get(args.command)
    if func:
        func(args)
    else:
        print(f"未知命令: {args.command}")

if __name__ == '__main__':
    main()
