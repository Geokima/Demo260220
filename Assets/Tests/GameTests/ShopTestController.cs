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

        private Rect _panelRect = new Rect(400, 100, 420, 550);
        private bool _showPanel = true;
        private int _currentTabIndex = 0;
        private List<string> _allShopTypes = new List<string>();

        private GUIStyle _panelStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
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
                fontSize = 12,
                normal = { background = CreateTex(420, 550, new Color(0.2f, 0.2f, 0.2f, 1f)) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { background = CreateTex(2, 2, new Color(0.35f, 0.35f, 0.35f, 1f)) },
                hover = { background = CreateTex(2, 2, new Color(0.45f, 0.45f, 0.45f, 1f)) },
                active = { background = CreateTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f)) }
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
            if (Input.GetKeyDown(KeyCode.F3))
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
                    if (_allShopTypes.Count > 0 && !_shopCache.ContainsKey(_allShopTypes[0]))
                    {
                        LoadShopInternal(_allShopTypes[0]);
                    }
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
                _panelRect = GUI.Window(3, _panelRect, DrawShopWindow, "商店系统 (F3开关)", _panelStyle);
            }
        }

        /// <summary> 渲染商店窗口 </summary>
        private void DrawShopWindow(int windowId)
        {
            GUILayout.BeginVertical();

            if (_allShopTypes.Count > 0)
            {
                int newIndex = GUILayout.SelectionGrid(_currentTabIndex, _allShopTypes.ToArray(), 3, _buttonStyle);
                if (newIndex != _currentTabIndex)
                {
                    _currentTabIndex = newIndex;
                    string newType = _allShopTypes[_currentTabIndex];
                    if (!_shopCache.ContainsKey(newType))
                    {
                        LoadShopInternal(newType);
                    }
                }
            }

            if (_allShopTypes.Count == 0)
            {
                GUILayout.Label("未探测到任何商店类型，请检查配置或尝试按 F3 重新开启", _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            if (_currentTabIndex >= _allShopTypes.Count) _currentTabIndex = 0;
            string currentType = _allShopTypes[_currentTabIndex];

            GUILayout.Space(10);
            
            _shopCache.TryGetValue(currentType, out var currentData);

            if (currentData == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"刷新: {currentData.RefreshCount}/{currentData.MaxRefreshCount}", _labelStyle);
                GUILayout.FlexibleSpace();
                if (currentType == ShopType.Random && currentData.CanRefresh)
                {
                    if (GUILayout.Button("刷新随机商店", _buttonStyle, GUILayout.Width(100)))
                    {
                        RefreshShopInternal(currentType);
                    }
                }
                GUILayout.EndHorizontal();

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                foreach (var item in currentData.Items)
                {
                    GUILayout.BeginVertical("box");

                    var discountStr = item.Discount < 1f ? $"[{item.Discount * 10:F1}折] " : "";
                    var itemName = GetItemName(item.ItemId);
                    GUILayout.Label($"{discountStr}{itemName} x{item.ItemCount}", _labelStyle);
                    
                    GUILayout.Label($"价格: {item.PriceType} {item.Price}", _labelStyle);

                    var limitStr = item.LimitCount > 0 ? $" / {item.LimitCount}" : " (不限)";
                    GUILayout.Label($"累计购买: {item.PurchasedCount}{limitStr}", _labelStyle);

                    if (item.CanBuy)
                    {
                        if (GUILayout.Button("购买", _buttonStyle, GUILayout.Width(80), GUILayout.Height(25)))
                        {
                            BuyItemInternal(item.ShopItemId, currentType);
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button("售罄", _buttonStyle, GUILayout.Width(80), GUILayout.Height(25));
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
            _lastMsg = "正在获取网络数据...";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopListRequest, ShopListResponse>("/shop/list", new ShopListRequest { ShopType = shopType });

                if (resp.Code == 0)
                {
                    _shopCache[shopType] = resp.Data;
                    _lastMsg = $"加载 [{shopType}] 成功";
                }
                else
                {
                    _lastMsg = $"错误: {resp.Msg}";
                }
            }
            catch (System.Exception e)
            {
                _lastMsg = $"异常: {e.Message}";
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
            _lastMsg = "正在请求刷新...";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopRefreshRequest, ShopRefreshResponse>("/shop/refresh", new ShopRefreshRequest { ShopType = shopType });

                if (resp.Code == 0)
                {
                    _shopCache[shopType] = resp.Data;
                    _lastMsg = "随机商店刷新完毕";
                }
                else
                {
                    _lastMsg = $"刷新失败: {resp.Msg}";
                }
            }
            catch (System.Exception e)
            {
                _lastMsg = $"异常: {e.Message}";
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
            _lastMsg = "购买处理中...";

            try
            {
                var gateway = this.GetSystem<IServerGateway>();
                var resp = await gateway.PostAsync<ShopBuyRequest, ShopBuyResponse>("/shop/buy", new ShopBuyRequest { ShopItemId = shopItemId, Count = 1 });

                if (resp.Code == 0)
                {
                    _lastMsg = "购买成功!";
                    if (resp.Data.ShopSync != null)
                        _shopCache[currentShopType] = resp.Data.ShopSync;
                }
                else
                {
                    _lastMsg = $"购买失败: {resp.Msg}";
                }
            }
            catch (System.Exception e)
            {
                _lastMsg = $"系统异常: {e.Message}";
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
