@echo off
chcp 65001 >nul
cd /d "%~dp0"

if "%~1"=="" (
    echo ==========================================
    echo GM 管理工具
    echo ==========================================
    echo.
    echo 用法: gm.bat ^<命令^> [参数]
    echo.
    echo 命令大全:
    echo   list                          列出所有玩家
    echo   query ^<username^>             查询玩家详情
    echo   give_diamond ^<username^> ^<amount^>     发放钻石
    echo   give_gold ^<username^> ^<amount^>        发放金币
    echo   give_item ^<username^> ^<item_id^> ^<amount^> [--bind]  发放物品
    echo   remove_item ^<username^> ^<uid^>         删除物品
    echo   set_password ^<username^> ^<password^>   修改密码
    echo.
    echo 示例:
    echo   gm.bat list
    echo   gm.bat query test
    echo   gm.bat give_diamond test 1000
    echo   gm.bat give_gold test 5000
    echo   gm.bat give_item test 1001 5
    echo   gm.bat give_item test 1001 5 --bind
    echo.
    pause
    exit /b
)

python gm_tool.py %*
