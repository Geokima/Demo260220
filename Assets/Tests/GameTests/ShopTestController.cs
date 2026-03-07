using System.Collections.Generic;
using Framework;
using Framework.Modules.Config;
using Game.Auth;
using Game.Base;
using Game.Config;
using Game.Consts;
using Game.DTOs;
using Game.Gateways;
using UnityEngine;

namespace Game.Tests
{
    public class ShopTestController : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; } = GameArchitecture.Instance;

        private Rect _panelRect = new Rect(600, 100, 400, 500);
        private bool _showPanel = true;
        private int _currentTab = 0;
        private string[] _tabs = { "固定商店", "随机商店" };

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _flatBtnStyle;
        private bool _initialized;
        private Vector2 _scrollPosition;

        private ShopListData _fixedShopData;
        private ShopListData _randomShopData;
        private string _pendingMsg = "";
        private string _lastMsg = "";
        private bool _isLoading = false;
        private string _pendingLoadShopType = null;
        private string _pendingRefreshShopType = null;
        private int _pendingBuyShopItemId = 0;

        private void Awake()
        {
            _initialized = false;
        }

        private void InitGUIStyles()
        {
            _panelStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 14,
                normal = { background = CreateTex(400, 500, new Color(0.12f, 0.12f, 0.15f, 0.95f)) }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _flatBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { background = CreateTex(2, 2, new Color(0.25f, 0.4f, 0.55f, 1f)) },
                hover = { background = CreateTex(2, 2, new Color(0.35f, 0.5f, 0.65f, 1f)) },
                active = { background = CreateTex(2, 2, new Color(0.2f, 0.35f, 0.5f, 1f)) }
            };

            _initialized = true;
        }

        private Texture2D CreateTex(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                _showPanel = !_showPanel;
            }

            ProcessPendingActions();
        }

        private void ProcessPendingActions()
        {
            if (_pendingLoadShopType != null)
            {
                var shopType = _pendingLoadShopType;
                _pendingLoadShopType = null;
                LoadShopInternal(shopType);
            }
            else if (_pendingRefreshShopType != null)
            {
                var shopType = _pendingRefreshShopType;
                _pendingRefreshShopType = null;
                RefreshShopInternal(shopType);
            }
            else if (_pendingBuyShopItemId > 0)
            {
                var shopItemId = _pendingBuyShopItemId;
                _pendingBuyShopItemId = 0;
                BuyItemInternal(shopItemId);
            }
        }

        private void OnGUI()
        {
            if (!_initialized)
                InitGUIStyles();

            if (GameArchitecture.Instance == null) return;

            var accountModel = this.GetModel<AccountModel>();
            if (accountModel == null || !accountModel.IsLoggedIn) return;

            if (_showPanel)
            {
                _panelRect = GUI.Window(3, _panelRect, DrawShopWindow, "", _panelStyle);
            }
        }

        private void DrawShopWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("商店系统 (F2开关)", _titleStyle);
            GUILayout.Space(5);

            _currentTab = GUILayout.SelectionGrid(_currentTab, _tabs, 2, _flatBtnStyle);
            GUILayout.Space(5);

            var shopType = _currentTab == 0 ? ShopType.Fixed : ShopType.Random;
            var shopData = _currentTab == 0 ? _fixedShopData : _randomShopData;

            if (shopData == null)
            {
                if (GUILayout.Button("加载商店", _flatBtnStyle, GUILayout.Height(30)))
                {
                    _pendingLoadShopType = shopType;
                }
            }
            else
            {
                GUILayout.Label($"<color=#FFD700>刷新次数: {shopData.RefreshCount}/{shopData.MaxRefreshCount}</color>");
                if (shopType == ShopType.Random && shopData.CanRefresh)
                {
                    if (GUILayout.Button("刷新随机商店", _flatBtnStyle, GUILayout.Height(30)))
                    {
                        _pendingRefreshShopType = shopType;
                    }
                }
                GUILayout.Space(5);

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                foreach (var item in shopData.Items)
                {
                    GUILayout.BeginVertical("box");

                    var discountStr = item.Discount < 1f ? $"<color=red>[{item.Discount:P0}]</color> " : "";
                    var itemName = GetItemName(item.ItemId);
                    GUILayout.Label($"<color=yellow>{discountStr}{itemName} x{item.ItemCount}</color>");
                    GUILayout.Label($"<color=white>价格: {item.PriceType} {item.Price}</color>");

                    if (item.OriginalPrice > item.Price)
                    {
                        GUILayout.Label($"<color=gray><s>原价: {item.OriginalPrice}</s></color>");
                    }

                    var limitStr = item.LimitCount > 0 ? $" 限购{item.LimitCount}" : "";
                    GUILayout.Label($"<color=gray>已购: {item.PurchasedCount}{limitStr}</color>");

                    GUILayout.BeginHorizontal();
                    if (item.CanBuy)
                    {
                        if (GUILayout.Button("购买", _flatBtnStyle, GUILayout.Width(100)))
                        {
                            _pendingBuyShopItemId = item.ShopItemId;
                        }
                    }
                    else
                    {
                        GUILayout.Label("<color=red>不可购买</color>");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }

                GUILayout.EndScrollView();

                if (GUILayout.Button("刷新商店", _flatBtnStyle, GUILayout.Height(30)))
                {
                    _pendingLoadShopType = shopType;
                }
            }

            if (!string.IsNullOrEmpty(_lastMsg))
            {
                GUILayout.Space(5);
                GUILayout.Label(_lastMsg);
            }

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, _panelRect.width, 25));
        }

        private async void LoadShopInternal(string shopType)
        {
            _isLoading = true;
            _lastMsg = "加载中...";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopListRequest, ShopListResponse>("/shop/list", new ShopListRequest { ShopType = shopType });

                if (resp.Code == 0)
                {
                    if (shopType == ShopType.Fixed)
                        _fixedShopData = resp.Data;
                    else
                        _randomShopData = resp.Data;

                    _lastMsg = "<color=green>加载成功</color>";
                }
                else
                {
                    _lastMsg = $"<color=red>加载失败: {resp.Msg}</color>";
                }
            }
            catch (System.Exception e)
            {
                _lastMsg = $"<color=red>异常: {e.Message}</color>";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void RefreshShopInternal(string shopType)
        {
            _isLoading = true;
            _lastMsg = "刷新中...";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopRefreshRequest, ShopRefreshResponse>("/shop/refresh", new ShopRefreshRequest { ShopType = shopType });

                if (resp.Code == 0)
                {
                    if (shopType == ShopType.Fixed)
                        _fixedShopData = resp.Data;
                    else
                        _randomShopData = resp.Data;

                    _lastMsg = "<color=green>刷新成功</color>";
                }
                else
                {
                    _lastMsg = $"<color=red>刷新失败: {resp.Msg}</color>";
                }
            }
            catch (System.Exception e)
            {
                _lastMsg = $"<color=red>异常: {e.Message}</color>";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private string GetItemName(int itemId)
        {
            try
            {
                var configSystem = this.GetSystem<IConfigSystem>();
                var itemConfig = configSystem?.Get<ItemConfig>(itemId);
                return itemConfig?.Name ?? $"物品{itemId}";
            }
            catch
            {
                return $"物品{itemId}";
            }
        }

        private async void BuyItemInternal(int shopItemId)
        {
            _isLoading = true;
            _lastMsg = "购买中...";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopBuyRequest, ShopBuyResponse>("/shop/buy", new ShopBuyRequest { ShopItemId = shopItemId, Count = 1 });

                if (resp.Code == 0)
                {
                    _lastMsg = "<color=green>购买成功</color>";
                    var shopType = _currentTab == 0 ? ShopType.Fixed : ShopType.Random;
                    _pendingLoadShopType = shopType;
                }
                else
                {
                    _lastMsg = $"<color=red>购买失败: {resp.Msg}</color>";
                }
            }
            catch (System.Exception e)
            {
                _lastMsg = $"<color=red>异常: {e.Message}</color>";
            }
            finally
            {
                _isLoading = false;
            }
        }

        public T GetModel<T>() where T : class, IModel => GameArchitecture.Instance.GetModel<T>();
        public T GetSystem<T>() where T : class, ISystem => GameArchitecture.Instance.GetSystem<T>();
        public void SendCommand<T>(T command) where T : ICommand => GameArchitecture.Instance.SendCommand(this, command);
        public void SendEvent<T>(T @event) where T : struct, IEvent => GameArchitecture.Instance.SendEvent(this, @event);
    }
}
