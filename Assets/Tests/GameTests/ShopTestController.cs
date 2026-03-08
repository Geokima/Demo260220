using System;
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
using Game.Procedures;
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

        /// <summary> 初始事件监听注册 </summary>
        private void Awake()
        {
            _initialized = false;
            // 弃用 Awake/Start 盲刷，改为精准监听加载完成事件
            Architecture.RegisterEvent<PreloadCompleteEvent>(OnPreloadComplete);
        }

        private void OnDestroy()
        {
            Architecture.UnregisterEvent<PreloadCompleteEvent>(OnPreloadComplete);
        }

        private void Start()
        {
            // 如果脚本挂载时加载已完成，Start 时主动触发一次
            RefreshAvailableShopTypes(); 
        }

        private void OnPreloadComplete(PreloadCompleteEvent e)
        {
            RefreshAvailableShopTypes();
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

        /// <summary> 动态从配置表中探测“当前时间段内有效”的商店分类 </summary>
        private void RefreshAvailableShopTypes()
        {
            var configSystem = this.GetSystem<IConfigSystem>();
            var sheet = configSystem?.GetSheet<ShopItemConfig>();
            if (sheet != null)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // 核心修复：前端也要同步时间过滤，避免显示早已过期或未到时间的商店页签
                var types = sheet.All()
                    .Where(c => c.StartTime <= now && (c.EndTime <= 0 || c.EndTime > now))
                    .Select(c => c.ShopType)
                    .Distinct()
                    .ToList();

                if (types.Count > 0)
                {
                    _allShopTypes = types;
                    return;
                }
            }
            
            _allShopTypes = new List<string>();
        }

        private void OnGUI()
        {
            if (!_initialized)
                InitGUIStyles();

            if (GameArchitecture.Instance == null) return;

            // 懒加载：如果开启了面板但类型列表还是空的，尝试重新探测（处理配置异步加载延迟）
            if (_showPanel && (_allShopTypes == null || _allShopTypes.Count == 0))
            {
                RefreshAvailableShopTypes();
            }

            var accountModel = this.GetModel<AccountModel>();
            if (accountModel == null || !accountModel.IsLoggedIn) return;

            if (_showPanel)
            {
                _panelRect = GUI.Window(3, _panelRect, DrawShopWindow, "", _panelStyle);
            }
        }

        /// <summary> 渲染商店窗口 </summary>
        private void DrawShopWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("商店系统 (F2开关 | 全类适配)", _titleStyle);
            GUILayout.Space(5);

            if (_allShopTypes.Count > 0)
            {
                _currentTabIndex = GUILayout.SelectionGrid(_currentTabIndex, _allShopTypes.ToArray(), 3, _flatBtnStyle);
            }

            if (_allShopTypes.Count == 0)
            {
                GUILayout.Label("<color=red>未探测到任何商店类型，请检查配置或尝试按 F2 重新开启</color>", _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            if (_currentTabIndex >= _allShopTypes.Count) _currentTabIndex = 0;
            string currentType = _allShopTypes[_currentTabIndex];

            GUILayout.Space(10);
            
            _shopCache.TryGetValue(currentType, out var currentData);

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
                    if (GUILayout.Button("刷新随机商店", _flatBtnStyle, GUILayout.Width(100)))
                    {
                        RefreshShopInternal(currentType);
                    }
                }
                if (GUILayout.Button("强制同步", _flatBtnStyle, GUILayout.Width(80)))
                {
                    LoadShopInternal(currentType);
                }
                GUILayout.EndHorizontal();

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                foreach (var item in currentData.Items)
                {
                    GUILayout.BeginVertical("box");

                    var discountStr = item.Discount < 1f ? $"<color=red>[{item.Discount * 10:F1}折]</color> " : "";
                    var itemName = GetItemName(item.ItemId);
                    GUILayout.Label($"<color=white>{discountStr}</color><color=yellow>{itemName}</color> <color=#999>x{item.ItemCount}</color>", _labelStyle);
                    
                    var priceColor = item.PriceType == CurrencyType.Gold ? "#FFD700" : "#00BFFF";
                    GUILayout.Label($"价格: <color={priceColor}>{item.PriceType} {item.Price}</color>", _labelStyle);

                    var limitStr = item.LimitCount > 0 ? $" / {item.LimitCount}" : " (不限)";
                    GUILayout.Label($"累计购买: <color=#32CD32>{item.PurchasedCount}</color>{limitStr}", _labelStyle);

                    if (item.CanBuy)
                    {
                        if (GUILayout.Button("购买", _flatBtnStyle, GUILayout.Width(80), GUILayout.Height(25)))
                        {
                            BuyItemInternal(item.ShopItemId, currentType);
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button("售罄", GUILayout.Width(80), GUILayout.Height(25));
                        GUI.enabled = true;
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }

                GUILayout.EndScrollView();
            }

            if (!string.IsNullOrEmpty(_lastMsg))
            {
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
