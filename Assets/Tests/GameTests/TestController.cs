using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Game.Auth;
using Game.Config;
using Game.DTOs;
using Game.Inventory;
using Game.Player;
using Game.Mission;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Tests
{
    public class TestController : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; } = GameArchitecture.Instance;

        private void Start()
        {
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            this.RegisterEvent<InventorySyncEvent>(OnInventorySync);
            this.RegisterEvent<ItemUsedEvent>(OnItemUsed);
        }

        #region Resource Buttons

        [Button("增加金币(+1000)", "GM")]
        private void AddGoldGM() => SendAddItem(1, 1000);

        [Button("增加钻石(+100)", "GM")]
        private void AddDiamondGM() => SendAddItem(2, 100);

        [Button("添加金币礼包(1003)", "GM")]
        private void AddItem1003() => SendAddItem(1003, 1);

        [Button("增加经验(+100)", "GM")]
        private void AddExpGM() { Debug.Log("经验模拟需通过任务或特定道具发放"); }

        private void SendAddItem(int itemId, int amount)
        {
            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn) { Debug.LogWarning("请先登录"); return; }
            this.SendCommand(new AddItemCommand { ItemId = itemId, Amount = amount });
        }

        #endregion

        #region Event Callbacks

        private void OnLoginSuccess(LoginSuccessEvent e)
        {
            Debug.Log($"<color=green>✓ 登录成功</color> UserId:{e.UserId}");
            BindDataPanel();
            _isDirty = true;
        }

        private void OnInventorySync(InventorySyncEvent e)
        {
            Debug.Log($"<color=green>✓ 背包同步: {e.SyncData.Reason}</color>");
            _isDirty = true;
        }

        private void OnItemUsed(ItemUsedEvent e)
        {
            Debug.Log($"<color=cyan>✓ 使用物品成功</color> UID:{e.Uid}");
            _isDirty = true;
        }

        #endregion

        #region GUI Panel Logic

        private bool _showPanel = true;
        private bool _isDirty = true;
        private Rect _panelRect = new Rect(20, 100, 320, 480);
        private Vector2 _scrollPosition;

        private int _cachedLevel, _cachedExp, _cachedEnergy, _cachedDiamond, _cachedGold, _cachedItemCount;
        private bool _isDataPanelBound = false;

        private void BindDataPanel()
        {
            if (_isDataPanelBound || GameArchitecture.Instance == null) return;
            var playerModel = this.GetModel<PlayerModel>();
            var inventoryModel = this.GetModel<InventoryModel>();
            if (playerModel == null || inventoryModel == null) return;

            _isDataPanelBound = true;
            playerModel.Level.Register(_ => _isDirty = true);
            playerModel.Exp.Register(_ => _isDirty = true);
            playerModel.Energy.Register(_ => _isDirty = true);
            playerModel.Diamond.Register(_ => _isDirty = true);
            playerModel.Gold.Register(_ => _isDirty = true);
            inventoryModel.Revision.Register(_ => _isDirty = true);
        }

        private void OnGUI()
        {
            SetupStyles();
            var accountModel = this.GetModel<AccountModel>();
            if (accountModel == null || !accountModel.IsLoggedIn) {
                DrawLoginBox();
                return;
            }

            if (_isDirty) RefreshCachedStats();
            _panelRect = GUI.Window(1, _panelRect, DrawStatsWindow, "玩家调试面板 (F1查看/隐藏)", _windowStyle);
        }

        private void RefreshCachedStats()
        {
            _isDirty = false;
            var playerModel = this.GetModel<PlayerModel>();
            var inventoryModel = this.GetModel<InventoryModel>();
            if (playerModel == null) return;

            _cachedLevel = playerModel.Level.Value;
            _cachedExp = playerModel.Exp.Value;
            _cachedEnergy = playerModel.Energy.Value;
            _cachedDiamond = playerModel.Diamond.Value;
            _cachedGold = playerModel.Gold.Value;
            _cachedItemCount = inventoryModel.GetAllItems().Count;
        }

        private void DrawStatsWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            
            var playerModel = this.GetModel<PlayerModel>();
            var (curExp, maxExp) = playerModel.GetLevelExpProgress(_cachedExp);
            float expPct = (float)curExp / maxExp;

            Label("等级", _cachedLevel.ToString(), "#FFD700");
            Label("经验", $"{curExp}/{maxExp} ({expPct:P1})", "#00CED1");
            Label("体力", $"{_cachedEnergy}/{playerModel.GetMaxEnergy()}", "#FF6347");
            Label("钻石", _cachedDiamond.ToString(), "#00BFFF");
            Label("金币", _cachedGold.ToString(), "#FFD700");
            Label("背包", $"{_cachedItemCount} Items", "#32CD32");

            GUILayout.Space(10);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
            var inventoryModel = this.GetModel<InventoryModel>();
            foreach (var item in inventoryModel.GetAllItems())
            {
                var cfg = this.GetSystem<IConfigSystem>().Get<ItemConfig>(item.ItemId);
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(cfg?.Name ?? $"Item:{item.ItemId}", GUILayout.Width(120));
                GUILayout.Label($"x{item.Count}");
                if (GUILayout.Button("使用", GUILayout.Width(50))) this.SendCommand(new UseItemCommand { Uid = item.Uid, Amount = 1 });
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("退出登录", GUILayout.Height(30))) this.SendCommand(new LogoutCommand());

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void Label(string key, string val, string color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(key, GUILayout.Width(60));
            GUILayout.Label($"<color={color}>{val}</color>");
            GUILayout.EndHorizontal();
        }

        private string _u = "test", _p = "123";
        private void DrawLoginBox()
        {
            Rect r = new Rect((Screen.width-200)/2, (Screen.height-150)/2, 200, 150);
            GUI.Box(r, "测试登录", _windowStyle);
            _u = GUI.TextField(new Rect(r.x+20, r.y+40, 160, 25), _u);
            _p = GUI.PasswordField(new Rect(r.x+20, r.y+75, 160, 25), _p, '*');
            if (GUI.Button(new Rect(r.x+20, r.y+110, 160, 25), "登 录")) this.SendCommand(new LoginCommand { Username = _u, Password = _p });
        }

        private GUIStyle _windowStyle;
        private void SetupStyles()
        {
            if (_windowStyle != null) return;
            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.richText = true;
        }

        #endregion

        public T GetModel<T>() where T : class, IModel => GameArchitecture.Instance.GetModel<T>();
        public T GetSystem<T>() where T : class, ISystem => GameArchitecture.Instance.GetSystem<T>();
        public void SendCommand<T>(T command) where T : ICommand => GameArchitecture.Instance.SendCommand(this, command);
        public void SendEvent<T>(T @event) where T : struct, IEvent => GameArchitecture.Instance.SendEvent(this, @event);
    }

    // 简易按钮属性
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class ButtonAttribute : System.Attribute {
        public string Name; public string Category;
        public ButtonAttribute(string n, string c) { Name = n; Category = c; }
    }
}
