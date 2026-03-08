using System.Collections.Generic;
using System.Linq;
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

        private Rect _panelRect = new Rect(600, 100, 420, 550);
        private bool _showPanel = true;
        private int _currentTabIndex = 0;
        private List<string> _allShopTypes = new List<string>();

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _flatBtnStyle;
        private GUIStyle _labelStyle;
        private bool _initialized;
        private Vector2 _scrollPosition;

        private Dictionary<string, ShopListData> _shopCache = new Dictionary<string, ShopListData>();
        private string _lastMsg = "";
        private bool _isLoading = false;

        private void Awake()
        {
            _initialized = false;
        }

        private void InitGUIStyles()
        {
            _panelStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 14,
                normal = { background = CreateTex(420, 550, new Color(0.12f, 0.12f, 0.15f, 0.95f)) }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan },
                alignment = TextAnchor.MiddleCenter
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                normal = { textColor = Color.white }
            };

            _flatBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { background = CreateTex(2, 2, new Color(0.2f, 0.35f, 0.5f, 1f)), textColor = Color.white },
                hover = { background = CreateTex(2, 2, new Color(0.3f, 0.45f, 0.6f, 1f)) },
                active = { background = CreateTex(2, 2, new Color(0.15f, 0.3f, 0.45f, 1f)) }
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
                if (_showPanel) RefreshAvailableShopTypes();
            }
        }

        private void RefreshAvailableShopTypes()
        {
            try
            {
                var configSystem = this.GetSystem<IConfigSystem>();
                var allConfigs = configSystem?.GetSheet<ShopItemConfig>()?.All();
                if (allConfigs != null)
                {
                    _allShopTypes = allConfigs.Select(c => c.ShopType).Distinct().ToList();
                }
            }
            catch { }

            if (_allShopTypes.Count == 0)
            {
                _allShopTypes.Add(ShopType.Fixed);
                _allShopTypes.Add(ShopType.Random);
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
            GUILayout.Label("商店系统 (F2开关 | 全类适配)", _titleStyle);
            GUILayout.Space(5);

            if (_allShopTypes.Count > 0)
            {
                _currentTabIndex = GUILayout.SelectionGrid(_currentTabIndex, _allShopTypes.ToArray(), 3, _flatBtnStyle);
            }

            if (_currentTabIndex >= _allShopTypes.Count) _currentTabIndex = 0;
            string currentType = _allShopTypes.Count > 0 ? _allShopTypes[_currentTabIndex] : ShopType.Fixed;

            GUILayout.Space(10);
            
            ShopListData currentData = _shopCache.GetValueOrDefault(currentType);

            if (currentData == null)
            {
                if (GUILayout.Button($"加载 [{currentType}] 商店", _flatBtnStyle, GUILayout.Height(35)))
                {
                    LoadShopInternal(currentType);
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color=#FFD700>刷新: {currentData.RefreshCount}/{currentData.MaxRefreshCount}</color>", _labelStyle);
                GUILayout.FlexibleSpace();
                if (currentType == ShopType.Random && currentData.CanRefresh)
                {
                    if (GUILayout.Button("手动刷新", _flatBtnStyle, GUILayout.Width(80)))
                    {
                        RefreshShopInternal(currentType);
                    }
                }
                if (GUILayout.Button("强制刷新列表", _flatBtnStyle, GUILayout.Width(100)))
                {
                    LoadShopInternal(currentType);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                foreach (var item in currentData.Items)
                {
                    GUILayout.BeginVertical("box");

                    var discountStr = item.Discount < 1f ? $"<color=red>[{item.Discount * 10:F1}折]</color> " : "";
                    var itemName = GetItemName(item.ItemId);
                    GUILayout.Label($"<color=white>{discountStr}</color><color=yellow>{itemName}</color> <color=white>x{item.ItemCount}</color>", _labelStyle);
                    
                    var priceColor = item.PriceType == CurrencyType.Gold ? "#FFD700" : "#00BFFF";
                    GUILayout.Label($"价格: <color={priceColor}>{item.PriceType} {item.Price}</color>", _labelStyle);

                    if (item.OriginalPrice > item.Price)
                    {
                        GUILayout.Label($"<color=gray><s>原价: {item.OriginalPrice}</s></color>", _labelStyle);
                    }

                    var limitStr = item.LimitCount > 0 ? $" / {item.LimitCount}" : " (不限)";
                    GUILayout.Label($"限购: <color=#32CD32>{item.PurchasedCount}</color>{limitStr}", _labelStyle);

                    GUILayout.BeginHorizontal();
                    if (item.CanBuy)
                    {
                        if (GUILayout.Button("购买", _flatBtnStyle, GUILayout.Width(100), GUILayout.Height(25)))
                        {
                            BuyItemInternal(item.ShopItemId, currentType);
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button("售罄/上限", GUILayout.Width(100), GUILayout.Height(25));
                        GUI.enabled = true;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(3);
                }

                GUILayout.EndScrollView();
            }

            if (!string.IsNullOrEmpty(_lastMsg))
            {
                GUILayout.Space(5);
                GUILayout.Label(_lastMsg, _labelStyle);
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, _panelRect.width, 25));
        }

        private async void LoadShopInternal(string shopType)
        {
            if (_isLoading) return;
            _isLoading = true;
            _lastMsg = "<color=gray>正在获取网络数据...</color>";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopListRequest, ShopListResponse>("/shop/list", new ShopListRequest { ShopType = shopType });

                if (resp.Code == 0)
                {
                    _shopCache[shopType] = resp.Data;
                    _lastMsg = $"<color=green>加载 [{shopType}] 成功</color>";
                }
                else
                {
                    _lastMsg = $"<color=red>错误: {resp.Msg}</color>";
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
            if (_isLoading) return;
            _isLoading = true;
            _lastMsg = "<color=gray>正在请求刷新...</color>";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopRefreshRequest, ShopRefreshResponse>("/shop/refresh", new ShopRefreshRequest { ShopType = shopType });

                if (resp.Code == 0)
                {
                    _shopCache[shopType] = resp.Data;
                    _lastMsg = "<color=green>随机商店刷新完毕</color>";
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

        private async void BuyItemInternal(int shopItemId, string currentShopType)
        {
            if (_isLoading) return;
            _isLoading = true;
            _lastMsg = "<color=gray>购买处理中...</color>";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopBuyRequest, ShopBuyResponse>("/shop/buy", new ShopBuyRequest { ShopItemId = shopItemId, Count = 1 });

                if (resp.Code == 0)
                {
                    _lastMsg = "<color=green>购买成功！</color>";
                    // 推送会自动更新模型，但测试面板通常需要立即同步 DTO 状态
                    if (resp.Data.ShopSync != null)
                        _shopCache[currentShopType] = resp.Data.ShopSync;
                }
                else
                {
                    _lastMsg = $"<color=red>购买失败: {resp.Msg}</color>";
                }
            }
            catch (System.Exception e)
            {
                _lastMsg = $"<color=red>系统异常: {e.Message}</color>";
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
                return itemConfig?.Name ?? $"ID:{itemId}";
            }
            catch
            {
                return $"ID:{itemId}";
            }
        }

        public T GetModel<T>() where T : class, IModel => GameArchitecture.Instance.GetModel<T>();
        public T GetSystem<T>() where T : class, ISystem => GameArchitecture.Instance.GetSystem<T>();
    }
}
