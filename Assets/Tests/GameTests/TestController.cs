using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Framework.Utils;
using Game.Auth;
using Game.Configs;
using Game.DTOs;
using Game.Inventory;
using Game.Player;
using UnityEngine;

namespace Game.Tests
{
    public class TestController : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; } = GameArchitecture.Instance;

        #region 生命周期

        private void Awake()
        {
            // GUI样式在OnGUI中延迟初始化
        }

        private void OnEnable()
        {
            // 重新激活时重置GUI样式，强制重新初始化
            _panelStyle = null;
        }

        private void Start()
        {
            RegisterEvents();
        }

        #endregion

        #region 本地体力恢复计算

        private long _lastLocalRecoverTime = 0;

        private void Update()
        {
            // 本地计算体力恢复（每10秒1点）
            if (GameArchitecture.Instance != null)
            {
                var playerModel = this.GetModel<PlayerModel>();
                var accountModel = this.GetModel<AccountModel>();
                if (playerModel != null && accountModel != null && accountModel.IsLoggedIn)
                {
                    var currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (_lastLocalRecoverTime == 0)
                    {
                        _lastLocalRecoverTime = currentTime;
                    }

                    long maxEnergy = playerModel.GetMaxEnergy();
                    if (playerModel.Energy.Value < maxEnergy)
                    {
                        long elapsed = currentTime - _lastLocalRecoverTime;
                        if (elapsed >= PlayerModel.EnergyRecoverInterval)
                        {
                            int recoverPoints = (int)(elapsed / PlayerModel.EnergyRecoverInterval);
                            int newEnergy = System.Math.Min(playerModel.Energy.Value + recoverPoints, (int)maxEnergy);
                            playerModel.Energy.Value = newEnergy;
                            _lastLocalRecoverTime = currentTime;
                            Debug.Log($"[TestController] 本地体力恢复: +{recoverPoints} → {newEnergy}/{maxEnergy}");
                        }
                    }
                    else
                    {
                        _lastLocalRecoverTime = currentTime;
                    }
                }
            }
        }

        #endregion

        #region 事件注册

        private void RegisterEvents()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            this.RegisterEvent<LoginFailedEvent>(OnLoginFailed);
            this.RegisterEvent<RegisterSuccessEvent>(OnRegisterSuccess);
            this.RegisterEvent<RegisterFailedEvent>(OnRegisterFailed);
            this.RegisterEvent<InventoryUpdatedEvent>(OnInventoryUpdated);
            this.RegisterEvent<ItemAddedEvent>(OnItemAdded);
            this.RegisterEvent<ItemRemovedEvent>(OnItemRemoved);
            this.RegisterEvent<ItemUsedEvent>(OnItemUsed);
            this.RegisterEvent<ItemOperationFailedEvent>(OnItemOperationFailed);
        }

        #endregion

        #region 登录测试

        [Button("测试账号", "登录")]
        private void TestLogin()
        {
            Debug.Log("<color=cyan>▶ 登录测试账号 (test)</color>");
            this.SendCommand(new LoginCommand { Username = "test", Password = "123" });
        }

        [Button("管理员账号", "登录")]
        private void TestLoginAdmin()
        {
            Debug.Log("<color=cyan>▶ 登录管理员账号 (admin)</color>");
            this.SendCommand(new LoginCommand { Username = "admin", Password = "123" });
        }

        #endregion

        #region 资源测试

        [Button("查询服务器资源", "资源")]
        private void TestGetResources()
        {
            Debug.Log("<color=yellow>▶ 查询服务器资源</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourceService = this.GetSystem<PlayerService>();
            GetResourcesAsync(resourceService).Forget();
        }

        [Button("增加钻石(+100)", "资源")]
        private void TestAddDiamond()
        {
            Debug.Log("<color=green>▶ 增加钻石 (+100)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Diamond.Value}</color>");
        }

        [Button("花费钻石(-50)", "资源")]
        private void TestSpendDiamond()
        {
            Debug.Log("<color=orange>▶ 花费钻石 (-50)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Diamond.Value}</color>");
        }

        [Button("增加金币(+500)", "资源")]
        private void TestAddGold()
        {
            Debug.Log("<color=green>▶ 增加金币 (+500)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Gold.Value}</color>");
        }

        [Button("花费金币(-200)", "资源")]
        private void TestSpendGold()
        {
            Debug.Log("<color=orange>▶ 花费金币 (-200)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Gold.Value}</color>");
        }

        [Button("增加经验(+100)", "资源")]
        private void TestAddExp()
        {
            Debug.Log("<color=green>▶ 增加经验 (+100)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var playerModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>经验{playerModel.Exp.Value} 等级{playerModel.Level.Value}</color>");
        }

        [Button("增加体力(+20)", "资源")]
        private void TestAddEnergy()
        {
            Debug.Log("<color=green>▶ 增加体力 (+20)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var playerModel = this.GetModel<PlayerModel>();
            var maxEnergy = playerModel.GetMaxEnergy();
            Debug.Log($"当前: <color=white>{playerModel.Energy.Value}/{maxEnergy}</color>");
        }

        [Button("消耗体力(-10)", "资源")]
        private void TestSpendEnergy()
        {
            Debug.Log("<color=orange>▶ 消耗体力 (-10)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var playerModel = this.GetModel<PlayerModel>();
            var maxEnergy = playerModel.GetMaxEnergy();
            Debug.Log($"当前: <color=white>{playerModel.Energy.Value}/{maxEnergy}</color>");
        }

        [Button("查询状态", "资源")]
        private void TestQueryStatus()
        {
            Debug.Log("<color=yellow>▶ 查询玩家状态</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var playerModel = this.GetModel<PlayerModel>();
            var maxEnergy = playerModel.GetMaxEnergy();
            var (curExp, maxExp) = playerModel.GetLevelExpProgress(playerModel.Exp.Value);

            Debug.Log($"<color=cyan>玩家状态</color>");
            Debug.Log($"  等级: <color=white>{playerModel.Level.Value}</color>");
            Debug.Log($"  经验: <color=white>{curExp}</color> / {maxExp} (当前等级)");
            Debug.Log($"  体力: <color=white>{playerModel.Energy.Value}</color> / {maxEnergy}");
            Debug.Log($"  钻石: <color=cyan>{playerModel.Diamond.Value}</color>");
            Debug.Log($"  金币: <color=yellow>{playerModel.Gold.Value}</color>");
        }

        #endregion

        #region 背包测试

        [Button("查询背包", "背包")]
        private void TestGetInventory()
        {
            Debug.Log("<color=yellow>▶ 查询背包</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            this.SendCommand(new GetInventoryCommand());
        }

        [Button("添加生命药水(x5)", "背包")]
        private void TestAddItem()
        {
            Debug.Log("<color=green>▶ 添加生命药水 (1001 x5)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            this.SendCommand(new AddItemCommand { ItemId = 1001, Amount = 5, Bind = false });
        }

        [Button("添加魔法药水(x3)", "背包")]
        private void TestAddItem2()
        {
            Debug.Log("<color=green>▶ 添加魔法药水 (1002 x3)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            this.SendCommand(new AddItemCommand { ItemId = 1002, Amount = 3, Bind = false });
        }

        [Button("添加绑定强化石(x10)", "背包")]
        private void TestAddBindItem()
        {
            Debug.Log("<color=green>▶ 添加绑定强化石 (1003 x10)</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            this.SendCommand(new AddItemCommand { ItemId = 1003, Amount = 10, Bind = true });
        }

        [Button("使用第一个物品", "背包")]
        private void TestUseFirstItem()
        {
            Debug.Log("<color=cyan>▶ 使用第一个物品</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var inventoryModel = this.GetModel<InventoryModel>();
            if (inventoryModel.Items.Count == 0)
            {
                Debug.LogWarning("<color=red>背包为空!</color>");
                return;
            }

            var firstItem = inventoryModel.Items[0];
            Debug.Log($"使用: {firstItem.uid} (ID:{firstItem.itemId})");
            this.SendCommand(new UseItemCommand { Uid = firstItem.uid, Amount = 1 });
        }

        [Button("移除第一个物品", "背包")]
        private void TestRemoveFirstItem()
        {
            Debug.Log("<color=orange>▶ 移除第一个物品</color>");

            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var inventoryModel = this.GetModel<InventoryModel>();
            if (inventoryModel.Items.Count == 0)
            {
                Debug.LogWarning("<color=red>背包为空!</color>");
                return;
            }

            var firstItem = inventoryModel.Items[0];
            Debug.Log($"移除: {firstItem.uid} (ID:{firstItem.itemId})");
            this.SendCommand(new RemoveItemCommand { Uid = firstItem.uid, Amount = 1 });
        }

        #endregion

        #region 事件回调

        private void OnLoginSuccess(LoginSuccessEvent e)
        {
            Debug.Log($"<color=green>✓ 登录成功</color> UserId:{e.UserId}");
            BindDataPanel();
            _isDirty = true; // 强制刷新缓存
        }

        private void OnLoginFailed(LoginFailedEvent e)
        {
            Debug.Log($"<color=red>✗ 登录失败: {e.Error}</color>");
            _loginError = e.Error;
        }

        private void OnRegisterSuccess(RegisterSuccessEvent e)
        {
            Debug.Log($"<color=green>✓ 注册成功</color> UserId:{e.UserId}");
            _loginError = "注册成功，请登录";
            _isRegisterMode = false;
        }

        private void OnRegisterFailed(RegisterFailedEvent e)
        {
            Debug.Log($"<color=red>✗ 注册失败: {e.Error}</color>");
            _loginError = e.Error;
        }

        private void OnInventoryUpdated(InventoryUpdatedEvent e)
        {
            Debug.Log($"<color=green>✓ 背包更新</color> 物品数:{e.Inventory.items?.Length ?? 0} 格子:{e.Inventory.maxSlots}");
            if (e.Inventory.items != null)
            {
                foreach (var item in e.Inventory.items)
                {
                    var itemConfig = this.GetSystem<IConfigSystem>().Get<ItemConfig>(item.itemId);
                    var itemName = itemConfig?.Name ?? $"物品{item.itemId}";
                    Debug.Log($"  - {itemName} x{item.count} [ID:{item.itemId}] 绑定:{item.bind}");
                }
            }
        }

        private void OnItemAdded(ItemAddedEvent e)
        {
            Debug.Log($"<color=green>✓ 获得物品</color> ID:{e.ItemId} 数量:{e.Amount}");
        }

        private void OnItemRemoved(ItemRemovedEvent e)
        {
            Debug.Log($"<color=orange>✓ 移除物品</color> UID:{e.Uid} 数量:{e.Amount}");
        }

        private void OnItemUsed(ItemUsedEvent e)
        {
            Debug.Log($"<color=cyan>✓ 使用物品</color> UID:{e.Uid} 数量:{e.Amount} 类型:{e.Effect.Type}");
        }

        private void OnItemOperationFailed(ItemOperationFailedEvent e)
        {
            Debug.Log($"<color=red>✗ 物品操作失败: {e.Reason}</color>");
        }

        private async void TestSendAnnouncement()
        {
            string message = "这是来自 GM 面板的测试公告 " + System.DateTime.Now.ToString("HH:mm:ss");
            Debug.Log($"<color=cyan>▶ 发送测试公告: {message}</color>");

            var httpSystem = GameArchitecture.Instance.GetSystem<Framework.Modules.Http.IHttpSystem>();
            string url = "http://localhost:8080/admin/announce";
            string json = "{\"message\":\"" + message + "\"}";

            var result = await httpSystem.PostAsync(url, json);
            if (result != null)
                Debug.Log("<color=green>✓ 公告发送成功</color>");
            else
                Debug.LogError("✗ 公告发送失败");
        }

        private async void TestKickSelf()
        {
            var accountModel = this.GetModel<AccountModel>();
            if (!accountModel.IsLoggedIn) return;

            int userId = accountModel.UserId.Value;
            Debug.Log($"<color=red>▶ 正在请求强制下线 UID: {userId}</color>");

            var httpSystem = GameArchitecture.Instance.GetSystem<Framework.Modules.Http.IHttpSystem>();
            string url = "http://localhost:8080/admin/kick";
            string json = "{\"userId\":" + userId + ", \"reason\":\"GM面板测试强制下线\"}";

            var result = await httpSystem.PostAsync(url, json);
            if (result != null)
                Debug.Log("<color=green>✓ 强制下线请求已发送</color>");
            else
                Debug.LogError("✗ 强制下线请求失败");
        }

        private async UniTaskVoid GetResourcesAsync(PlayerService resourceService)
        {
            var response = await resourceService.GetResourcesAsync();
            if (response != null && response.Code == 0)
            {
                var data = response.Data;
                Debug.Log($"<color=green>✓</color> 钻石:<color=cyan>{data.Diamond}</color> 金币:<color=yellow>{data.Gold}</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ 查询失败</color>");
            }
        }

        #endregion

        #region OnGUI 数据面板

        private bool _showPanel = true;
        private bool _isDirty = true;
        private Rect _panelRect = new Rect(Screen.width - 330, Screen.height - 310, 320, 300);
        private Vector2 _scrollPosition;

        private int _cachedLevel;
        private int _cachedExp;
        private int _cachedEnergy;
        private int _cachedDiamond;
        private int _cachedGold;
        private int _cachedItemCount;

        private bool _isDataPanelBound = false;

        private void BindDataPanel()
        {
            if (_isDataPanelBound) return;
            if (GameArchitecture.Instance == null) return;

            var playerModel = this.GetModel<PlayerModel>();
            var inventoryModel = this.GetModel<InventoryModel>();
            if (playerModel == null || inventoryModel == null) return;

            _isDataPanelBound = true;

            playerModel.Level.Register(v => _isDirty = true);
            playerModel.Exp.Register(v => _isDirty = true);
            playerModel.Energy.Register(v => _isDirty = true);
            playerModel.Diamond.Register(v => _isDirty = true);
            playerModel.Gold.Register(v => _isDirty = true);
            inventoryModel.Items.OnCountChanged.Register(c => _isDirty = true);
            inventoryModel.Items.OnReplace.Register((i, old, @new) => _isDirty = true);
            inventoryModel.Items.OnAdd.Register((i, item) => _isDirty = true);
            inventoryModel.Items.OnRemove.Register((i, item) => _isDirty = true);
            inventoryModel.Items.OnClear.Register(() => _isDirty = true);
            inventoryModel.Items.OnMove.Register((oldI, newI, item) => _isDirty = true);
        }

        private void OnGUI()
        {
            // 延迟初始化GUI样式
            if (_panelStyle == null)
                InitGUIStyles();

            // 显示按钮（当面板关闭时，显示在右下角）
            if (!_showPanel)
            {
                if (GUI.Button(new Rect(Screen.width - 120, Screen.height - 40, 110, 30), "显示数据面板", _flatBtnStyle))
                    _showPanel = true;
            }

            if (!_showPanel) return;

            if (GameArchitecture.Instance == null) return;

            var accountModel = this.GetModel<AccountModel>();

            // 未登录显示登录面板（居中，不可拖动）
            if (accountModel != null && !accountModel.IsLoggedIn)
            {
                var loginRect = new Rect((Screen.width - 320) / 2, (Screen.height - 300) / 2, 320, 300);
                GUI.Window(1, loginRect, DrawLoginWindow, "", _panelStyle);
                return;
            }

            if (_isDirty)
            {
                _isDirty = false;
                var playerModel = this.GetModel<PlayerModel>();
                var inventoryModel = this.GetModel<InventoryModel>();

                // 检查模型是否可用
                if (playerModel == null || inventoryModel == null)
                    return;

                _cachedLevel = playerModel.Level.Value;
                _cachedExp = playerModel.Exp.Value;
                _cachedEnergy = playerModel.Energy.Value;
                _cachedDiamond = playerModel.Diamond.Value;
                _cachedGold = playerModel.Gold.Value;
                _cachedItemCount = inventoryModel.Items.Count;
            }

            // 可拖动窗口
            _panelRect = GUI.Window(1, _panelRect, DrawPanelWindow, "", _panelStyle);
        }

        private string _loginUsername = "";
        private string _loginPassword = "";
        private bool _isRegisterMode = false;
        private string _loginError = "";

        private void DrawLoginWindow(int windowId)
        {
            var title = _isRegisterMode ? "注册账号" : "账号登录";
            GUI.Label(new Rect(0, 0, _panelRect.width, 30), title, _titleStyle);

            var margin = 15;
            var fieldWidth = _panelRect.width - margin * 2;
            var fieldHeight = 32;
            var y = 45;

            // 账号输入
            GUI.Label(new Rect(margin, y, fieldWidth, 18), "账号", _labelStyle);
            y += 20;
            _loginUsername = GUI.TextField(new Rect(margin, y, fieldWidth, fieldHeight), _loginUsername, _flatTextFieldStyle);
            y += fieldHeight + 12;

            // 密码输入
            GUI.Label(new Rect(margin, y, fieldWidth, 18), "密码", _labelStyle);
            y += 20;
            _loginPassword = GUI.PasswordField(new Rect(margin, y, fieldWidth, fieldHeight), _loginPassword, '*', _flatTextFieldStyle);
            y += fieldHeight + 15;

            // 错误提示（固定空间，避免布局跳动）
            var errorColor = string.IsNullOrEmpty(_loginError) ? "<color=gray> </color>" : $"<color=red>{_loginError}</color>";
            GUI.Label(new Rect(margin, y, fieldWidth, 20), errorColor, _labelStyle);
            y += 25;

            // 主按钮
            var btnHeight = 38;
            var btnText = _isRegisterMode ? "注 册" : "登 录";
            if (GUI.Button(new Rect(margin, y, fieldWidth, btnHeight), btnText, _flatBtnStyle))
            {
                if (string.IsNullOrEmpty(_loginUsername) || string.IsNullOrEmpty(_loginPassword))
                {
                    _loginError = "账号密码不能为空";
                }
                else
                {
                    _loginError = "";
                    if (_isRegisterMode)
                        this.SendCommand(new RegisterCommand { Username = _loginUsername, Password = _loginPassword });
                    else
                        this.SendCommand(new LoginCommand { Username = _loginUsername, Password = _loginPassword });
                }
            }
            y += btnHeight + 15;

            // 切换模式（文字链接样式）
            var switchText = _isRegisterMode ? "<color=#66AAFF>已有账号? 去登录</color>" : "<color=#66AAFF>没有账号? 去注册</color>";
            if (GUI.Button(new Rect(margin, y, fieldWidth, 22), switchText, _labelStyle))
            {
                _isRegisterMode = !_isRegisterMode;
                _loginError = "";
            }
            // 登录窗口不可拖动
        }

        private void DrawPanelWindow(int windowId)
        {
            var playerModelRef = this.GetModel<PlayerModel>();
            var inventoryModelRef = this.GetModel<InventoryModel>();

            // 检查架构是否初始化
            if (playerModelRef == null || inventoryModelRef == null)
            {
                GUI.Label(new Rect(10, 30, 250, 50), "<color=red>框架未初始化...</color>", _labelStyle);
                GUI.DragWindow(new Rect(0, 0, _panelRect.width, 25));
                return;
            }

            // 标题栏（扁平风格）
            GUI.Label(new Rect(0, 0, _panelRect.width, 30), "玩家数据", _titleStyle);

            // 内容区域（留出底部按钮空间）
            var contentWidth = _panelRect.width - 20;
            var contentRect = new Rect(10, 35, contentWidth, _panelRect.height - 75);
            _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, new Rect(0, 0, contentWidth - 20, 350));

            float y = 0;
            var lineHeight = 22f;
            var itemWidth = contentWidth - 30;

            GUI.Label(new Rect(0, y, itemWidth, lineHeight), $"<color=#FFD700>等级:</color> {_cachedLevel}", _labelStyle);
            y += lineHeight;

            // 显示当前等级的经验进度
            var (curExp, maxExp) = playerModelRef.GetLevelExpProgress(_cachedExp);
            var expPercent = (float)curExp / maxExp * 100;
            GUI.Label(new Rect(0, y, itemWidth, lineHeight), $"<color=#00CED1>经验:</color> {curExp} / {maxExp} ({expPercent:F1}%)", _labelStyle);
            y += lineHeight;

            GUI.Box(new Rect(0, y, itemWidth, 10), "", _barBgStyle);
            GUI.Box(new Rect(0, y, itemWidth * expPercent / 100, 10), "", _expBarStyle);
            y += 15;

            var maxEnergy = playerModelRef.GetMaxEnergy();
            var energyPercent = (float)_cachedEnergy / maxEnergy * 100;
            GUI.Label(new Rect(0, y, itemWidth, lineHeight), $"<color=#FF6347>体力:</color> {_cachedEnergy} / {maxEnergy}", _labelStyle);
            y += lineHeight;

            GUI.Box(new Rect(0, y, itemWidth, 10), "", _barBgStyle);
            GUI.Box(new Rect(0, y, itemWidth * energyPercent / 100, 10), "", _energyBarStyle);
            y += 20;

            GUI.Label(new Rect(0, y, itemWidth, lineHeight), $"<color=#00BFFF>钻石:</color> {_cachedDiamond}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(0, y, itemWidth, lineHeight), $"<color=#FFD700>金币:</color> {_cachedGold}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(0, y, itemWidth, lineHeight), $"<color=#32CD32>背包:</color> {_cachedItemCount} 格", _labelStyle);
            y += lineHeight + 10;

            // 操作按钮（扁平样式）
            var btnWidth = (itemWidth - 10) / 2;
            var btnHeight2 = 28f;

            if (GUI.Button(new Rect(0, y, btnWidth, btnHeight2), "+100 钻石", _flatBtnStyle))
            {
            }
            if (GUI.Button(new Rect(btnWidth + 10, y, btnWidth, btnHeight2), "+500 金币", _flatBtnStyle))
            {
            }
            y += btnHeight2 + 8;

            if (GUI.Button(new Rect(0, y, btnWidth, btnHeight2), "+100 经验", _flatBtnStyle))
            {
            }
            if (GUI.Button(new Rect(btnWidth + 10, y, btnWidth, btnHeight2), "+20 体力", _flatBtnStyle))
            {
            }
            y += btnHeight2 + 8;

            if (GUI.Button(new Rect(0, y, btnWidth, btnHeight2), "1钻石→100金币", _flatBtnStyle))
            {
            }
            if (GUI.Button(new Rect(btnWidth + 10, y, btnWidth, btnHeight2), "添加药水", _flatBtnStyle))
            {
                this.SendCommand(new AddItemCommand { ItemId = 1001, Amount = 1, Bind = false });
            }
            y += btnHeight2 + 8;

            if (GUI.Button(new Rect(0, y, btnWidth, btnHeight2), "查询背包", _flatBtnStyle))
            {
                this.SendCommand(new GetInventoryCommand());
            }
            if (GUI.Button(new Rect(btnWidth + 10, y, btnWidth, btnHeight2), "发送公告", _flatBtnStyle))
            {
                TestSendAnnouncement();
            }
            y += btnHeight2 + 8;

            if (GUI.Button(new Rect(0, y, itemWidth, btnHeight2), "<color=red>强制踢掉自己 (WS测试)</color>", _flatBtnStyle))
            {
                TestKickSelf();
            }
            y += btnHeight2 + 8;

            GUI.EndScrollView();

            // 关闭按钮（扁平样式）
            var closeStyle = new GUIStyle(_flatBtnStyle);
            closeStyle.fontSize = 14;
            closeStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            closeStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            closeStyle.hover.background = MakeTex(2, 2, new Color(0.9f, 0.2f, 0.2f, 1f));
            closeStyle.hover.textColor = Color.white;
            closeStyle.active.background = MakeTex(2, 2, new Color(0.7f, 0.15f, 0.15f, 1f));
            if (GUI.Button(new Rect(_panelRect.width - 32, 2, 28, 28), "×", closeStyle))
            {
                _showPanel = false;
            }

            // 退出登录按钮（扁平样式，红色警示）
            var logoutStyle = new GUIStyle(_flatBtnStyle);
            logoutStyle.normal.background = MakeTex(2, 2, new Color(0.8f, 0.2f, 0.2f, 1f));
            logoutStyle.hover.background = MakeTex(2, 2, new Color(0.9f, 0.3f, 0.3f, 1f));
            logoutStyle.active.background = MakeTex(2, 2, new Color(0.7f, 0.15f, 0.15f, 1f));
            if (GUI.Button(new Rect(10, _panelRect.height - 45, _panelRect.width - 20, 35), "退出登录", logoutStyle))
            {
                this.SendCommand(new LogoutCommand());
                _isDataPanelBound = false;
            }

            // 使窗口可拖动
            GUI.DragWindow(new Rect(0, 0, _panelRect.width, 25));
        }

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _barBgStyle;
        private GUIStyle _expBarStyle;
        private GUIStyle _energyBarStyle;
        private GUIStyle _closeBtnStyle;
        private GUIStyle _flatBtnStyle;
        private GUIStyle _flatTextFieldStyle;

        private void InitGUIStyles()
        {
            _panelStyle = new GUIStyle(GUI.skin.window);
            _panelStyle.normal.background = MakeTex(280, 200, new Color(0.15f, 0.15f, 0.15f, 0.95f));
            _panelStyle.active.background = _panelStyle.normal.background;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 14;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            _titleStyle.alignment = TextAnchor.MiddleCenter;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 12;
            _labelStyle.richText = true;

            _barBgStyle = new GUIStyle(GUI.skin.box);
            _barBgStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 1f));

            _expBarStyle = new GUIStyle(GUI.skin.box);
            _expBarStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.8f, 0.9f, 1f));

            _energyBarStyle = new GUIStyle(GUI.skin.box);
            _energyBarStyle.normal.background = MakeTex(2, 2, new Color(1f, 0.4f, 0.3f, 1f));

            _closeBtnStyle = new GUIStyle(GUI.skin.button);
            _closeBtnStyle.fontSize = 16;
            _closeBtnStyle.fontStyle = FontStyle.Bold;
            _closeBtnStyle.normal.textColor = Color.red;

            // 扁平按钮样式
            _flatBtnStyle = new GUIStyle(GUI.skin.button);
            _flatBtnStyle.fontSize = 13;
            _flatBtnStyle.fontStyle = FontStyle.Bold;
            _flatBtnStyle.alignment = TextAnchor.MiddleCenter;
            _flatBtnStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.6f, 1f, 1f));
            _flatBtnStyle.normal.textColor = Color.white;
            _flatBtnStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.7f, 1f, 1f));
            _flatBtnStyle.hover.textColor = Color.white;
            _flatBtnStyle.active.background = MakeTex(2, 2, new Color(0.15f, 0.5f, 0.9f, 1f));
            _flatBtnStyle.active.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            _flatBtnStyle.border = new RectOffset(0, 0, 0, 0);
            _flatBtnStyle.padding = new RectOffset(10, 10, 8, 8);

            // 扁平输入框样式
            _flatTextFieldStyle = new GUIStyle(GUI.skin.textField);
            _flatTextFieldStyle.fontSize = 13;
            _flatTextFieldStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f));
            _flatTextFieldStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            _flatTextFieldStyle.hover.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 1f));
            _flatTextFieldStyle.hover.textColor = Color.white;
            _flatTextFieldStyle.focused.background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f));
            _flatTextFieldStyle.focused.textColor = Color.white;
            _flatTextFieldStyle.border = new RectOffset(0, 0, 0, 0);
            _flatTextFieldStyle.padding = new RectOffset(10, 10, 8, 8);
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        #endregion
    }
}
