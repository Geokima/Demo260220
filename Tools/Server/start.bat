@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ==========================================
echo 游戏服务器
echo ==========================================
echo.
echo HTTP API:   http://localhost:8080
echo WebSocket:  ws://localhost:8081
echo.
echo HTTP 接口:
echo   POST /login            - 登录
echo   POST /resource/get     - 获取资源
echo   POST /resource/diamond - 钻石变更
echo   POST /resource/gold    - 金币变更
echo   POST /resource/exp     - 经验变更
echo   POST /resource/energy  - 体力变更
echo   POST /inventory/get    - 获取背包
echo   POST /inventory/add    - 添加物品
echo   POST /inventory/remove - 移除物品
echo   POST /inventory/use    - 使用物品
echo.
echo WebSocket 消息:
echo   login       - 登录
echo   heartbeat   - 心跳
echo   chat        - 聊天（广播）
echo   player_sync - 玩家同步
echo   echo        - 回声测试
echo ==========================================
echo.

python main.py
