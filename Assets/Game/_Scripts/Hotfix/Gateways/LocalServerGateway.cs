using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Framework.Modules.Network;
using Game.Config;
using Game.Consts;
using Game.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Gateways
{
    [Serializable]
    public class LocalSaveData
    {
        public PlayerData Player = new();
        public InventoryData Inventory = new();
    }

    public class LocalServerGateway : AbstractSystem, IServerGateway
    {
        private readonly string _savePath = Path.Combine(Application.persistentDataPath, "local_save.json");

        public override void Init()
        {
            LoadSaveData();
        }

        public override void Deinit()
        {
            SaveSaveData();
        }

        public void Save() => SaveSaveData();

        private void LoadSaveData()
        {
            Debug.Log($"[LocalServerGateway] 从存档路径加载: {_savePath}");
            try
            {
                if (File.Exists(_savePath))
                {
                    var json = File.ReadAllText(_savePath);
                    _saveData = JsonConvert.DeserializeObject<LocalSaveData>(json) ?? new LocalSaveData();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[LocalServerGateway] 加载存档失败: {ex.Message}");
                _saveData = new LocalSaveData();
            }
            _saveData = new LocalSaveData()
            {
                Player = new PlayerData
                {
                    Diamond = 1000,
                    Gold = 5000,
                    Exp = 0,
                    Energy = 100
                },
                Inventory = new InventoryData
                {
                    MaxSlots = int.MaxValue,
                    Items = new[]
                {
                    new ItemData { Uid = "1001_1", ItemId = 1001, Count = 10 },
                    new ItemData { Uid = "1002_1", ItemId = 1002, Count = 10 },
                    new ItemData { Uid = "1003_1", ItemId = 1003, Count = 10 },
                    new ItemData { Uid = "1004_1", ItemId = 1004, Count = 10 }
                }
                }
            };
        }

        private void SaveSaveData()
        {
            Debug.Log($"[LocalServerGateway] 保存到存档路径: {_savePath}");
            try
            {
                var json = JsonConvert.SerializeObject(_saveData, Formatting.Indented);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalServerGateway] 保存存档失败: {ex.Message}");
            }
        }

        #region IServerGateway

        public string WsUrl { get; set; }
        public NetworkStatus WsStatus => NetworkStatus.Connected;

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request) where TResponse : class
        {
            object result = path switch
            {
                "/login" => HandleLogin(request as LoginRequest),
                "/register" => HandleRegister(request as RegisterRequest),
                "/logout" => HandleLogout(request as LogoutRequest),
                "/inventory/get" => HandleGetInventory(),
                "/inventory/add" => HandleAddItem(request as AddItemRequest),
                "/inventory/remove" => HandleRemoveItem(request as RemoveItemRequest),
                "/inventory/use" => HandleUseItem(request as UseItemRequest),
                "/resource/get" => HandleGetResource(),
                _ => throw new Exception($"[LocalServerGateway] 未实现路由: {path}")
            };
            await UniTask.Delay(10);
            return result as TResponse;
        }

        public async UniTask<TResponse> PostAsync<TResponse>(string path) where TResponse : class
        {
            return await PostAsync<object, TResponse>(path, new { });
        }

        #endregion

        #region Data

        private Dictionary<string, Action<JToken>> _wsHandlers = new();
        private LocalSaveData _saveData = new();

        #endregion

        #region Handlers

        private LoginResponse HandleLogin(LoginRequest req)
        {
            var request = req as LoginRequest;
            return new LoginResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new LoginData
                {
                    Token = "local_token_001",
                    UserId = 1,
                    Username = request?.Username ?? "Player",
                    WsUrl = ""
                }
            };
        }

        private RegisterResponse HandleRegister(RegisterRequest req)
        {
            return new RegisterResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new RegisterData { UserId = 1 }
            };
        }

        private LogoutResponse HandleLogout(LogoutRequest req)
        {
            return new LogoutResponse
            {
                Code = 0,
                Msg = "Success"
            };
        }

        private InventoryResponse HandleGetInventory()
        {
            return new InventoryResponse
            {
                Code = 0,
                Data = _saveData.Inventory
            };
        }

        // TODO 理论上不该直接添加物品，测试阶段保留
        private InventoryResponse HandleAddItem(AddItemRequest req)
        {
            if (req == null) return new InventoryResponse { Code = 1, Msg = "请求无效" };

            int itemId = req.ItemId;
            int amount = req.Amount;

            var configSystem = this.GetSystem<IConfigSystem>();
            var itemConfig = configSystem.Get<ItemConfig>(itemId);
            int maxStack = itemConfig?.MaxStack ?? 99;

            var items = _saveData.Inventory.Items?.ToList() ?? new List<ItemData>();
            int remaining = amount;

            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                var item = items[i];
                if (item.ItemId == itemId && item.Count < maxStack)
                {
                    int canAdd = maxStack - item.Count;
                    int add = Math.Min(canAdd, remaining);
                    item.Count += add;
                    remaining -= add;
                }
            }

            while (remaining > 0)
            {
                if (items.Count >= _saveData.Inventory.MaxSlots)
                    break;

                items.Add(new ItemData
                {
                    Uid = $"{itemId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{UnityEngine.Random.Range(1000, 9999)}",
                    ItemId = itemId,
                    Count = Math.Min(remaining, maxStack)
                });
                remaining -= Math.Min(remaining, maxStack);
            }

            _saveData.Inventory.Items = items.ToArray();
            // 模拟掉落同步
            SimulatePush(NetworkMsgType.InventoryUpdate, new InventorySyncData 
            { 
                ChangedItems = items.Where(x => amount > 0 && x.ItemId == itemId).ToList(), 
                Reason = InventorySyncReason.DROP 
            });

            Debug.Log($"[LocalServerGateway] 添加物品: itemId={itemId}, amount={amount}");

            return new InventoryResponse
            {
                Code = remaining > 0 ? 2 : 0,
                Msg = remaining > 0 ? "背包已满" : "Success",
                Data = _saveData.Inventory
            };
        }

        private InventoryResponse HandleRemoveItem(RemoveItemRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Uid))
                return new InventoryResponse { Code = 1, Msg = "UID无效" };

            var items = _saveData.Inventory.Items?.ToList();
            if (items == null)
                return new InventoryResponse { Code = 2, Msg = "背包为空" };

            var target = items.FirstOrDefault(x => x.Uid == req.Uid);
            if (target == null || target.Count < req.Amount)
                return new InventoryResponse { Code = 3, Msg = "物品数量不足" };

            target.Count -= req.Amount;

            var syncData = new InventorySyncData
            {
                ChangedItems = target.Count > 0 ? new List<ItemData> { target } : null,
                RemovedUids = target.Count <= 0 ? new List<string> { req.Uid } : null,
                Reason = InventorySyncReason.USE // 暂时通用为 USE
            };

            if (target.Count <= 0)
                items.Remove(target);

            _saveData.Inventory.Items = items.ToArray();
            SimulatePush(NetworkMsgType.InventoryUpdate, syncData);

            return new InventoryResponse { Code = 0, Msg = "Success", Data = _saveData.Inventory };
        }

        private UseItemResponse HandleUseItem(UseItemRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Uid))
                return new UseItemResponse { Code = 1, Msg = "UID无效" };

            var items = _saveData.Inventory.Items?.ToList();
            if (items == null)
                return new UseItemResponse { Code = 2, Msg = "背包为空" };

            var target = items.FirstOrDefault(x => x.Uid == req.Uid);
            if (target == null || target.Count < req.Amount)
                return new UseItemResponse { Code = 3, Msg = "物品数量不足" };

            target.Count -= req.Amount;

            var syncData = new InventorySyncData
            {
                ChangedItems = target.Count > 0 ? new List<ItemData> { target } : null,
                RemovedUids = target.Count <= 0 ? new List<string> { req.Uid } : null,
                Reason = InventorySyncReason.USE 
            };

            if (target.Count <= 0)
                items.Remove(target);

            _saveData.Inventory.Items = items.ToArray();
            SimulatePush(NetworkMsgType.InventoryUpdate, syncData);

            var configSystem = this.GetSystem<IConfigSystem>();
            var itemConfig = configSystem.Get<ItemConfig>(target.ItemId);
            var effectId = itemConfig?.EffectId > 0 ? itemConfig.EffectId.ToString() : "0";
            var effectParams = new Dictionary<string, string> { { "value", (itemConfig?.EffectId * 10).ToString() } };

            return new UseItemResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new List<ItemEffect>
                {
                    new ItemEffect { EffectId = effectId, Params = effectParams }
                }
            };
        }

        private PlayerResponse HandleGetResource()
        {
            return new PlayerResponse
            {
                Code = 0,
                Data = _saveData.Player
            };
        }

        #endregion

        #region WebSocket

        public UniTask<bool> ConnectWsAsync(string url = null)
        {
            return UniTask.FromResult(true);
        }

        public void DisconnectWs() { }

        public void SendWsMessage(string msgType, object data) { }

        public void RegisterWsHandler(string msgType, Action<JToken> handler)
        {
            _wsHandlers[msgType] = handler;
        }

        public void UnregisterWsHandler(string msgType, Action<JToken> handler)
        {
            _wsHandlers.Remove(msgType);
        }

        public void SimulatePush(string msgType, object data)
        {
            if (_wsHandlers.TryGetValue(msgType, out var handler))
            {
                handler.Invoke(JToken.FromObject(data));
            }
        }

        #endregion
    }
}
