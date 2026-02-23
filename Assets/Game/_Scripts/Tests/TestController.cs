using UnityEngine;
using Cysharp.Threading.Tasks;
using Game.Systems;
using Game.Commands;
using Game.Services;
using Game.Models;
using Framework;
using Framework.Utils;

namespace Game.Tests
{
    public class TestController : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; } = GameArchitecture.Instance;

        private void Start()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            this.RegisterEvent<LoginFailedEvent>(OnLoginFailed);
            this.RegisterEvent<DiamondChangedEvent>(OnDiamondChanged);
            this.RegisterEvent<DiamondChangeFailedEvent>(OnDiamondChangeFailed);
            this.RegisterEvent<GoldChangedEvent>(OnGoldChanged);
            this.RegisterEvent<GoldChangeFailedEvent>(OnGoldChangeFailed);
            this.RegisterEvent<InventoryUpdatedEvent>(OnInventoryUpdated);
            this.RegisterEvent<ItemAddedEvent>(OnItemAdded);
            this.RegisterEvent<ItemRemovedEvent>(OnItemRemoved);
            this.RegisterEvent<ItemUsedEvent>(OnItemUsed);
            this.RegisterEvent<ItemOperationFailedEvent>(OnItemOperationFailed);
        }

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

        [Button("查询", "资源")]
        private void TestGetResources()
        {
            Debug.Log("<color=yellow>▶ 查询玩家资源</color>");

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourceService = this.GetSystem<ResourceService>();
            GetResourcesAsync(resourceService).Forget();
        }

        [Button("增加钻石(+100)", "资源")]
        private void TestAddDiamond()
        {
            Debug.Log("<color=green>▶ 增加钻石 (+100)</color>");

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Diamond.Value}</color>");

            this.SendCommand(new ChangeDiamondCommand { Amount = 100, Reason = "测试增加" });
        }

        [Button("花费钻石(-50)", "资源")]
        private void TestSpendDiamond()
        {
            Debug.Log("<color=orange>▶ 花费钻石 (-50)</color>");

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Diamond.Value}</color>");

            this.SendCommand(new ChangeDiamondCommand { Amount = -50, Reason = "测试花费" });
        }

        [Button("增加金币(+500)", "资源")]
        private void TestAddGold()
        {
            Debug.Log("<color=green>▶ 增加金币 (+500)</color>");

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Gold.Value}</color>");

            this.SendCommand(new ChangeGoldCommand { Amount = 500, Reason = "测试增加" });
        }

        [Button("花费金币(-200)", "资源")]
        private void TestSpendGold()
        {
            Debug.Log("<color=orange>▶ 花费金币 (-200)</color>");

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
            {
                Debug.LogWarning("<color=red>请先登录!</color>");
                return;
            }

            var resourcesModel = this.GetModel<PlayerModel>();
            Debug.Log($"当前: <color=white>{resourcesModel.Gold.Value}</color>");

            this.SendCommand(new ChangeGoldCommand { Amount = -200, Reason = "测试花费" });
        }

        private void OnLoginSuccess(LoginSuccessEvent e)
        {
            Debug.Log($"<color=green>✓ 登录成功</color> UserId:{e.UserId}");
        }

        private void OnLoginFailed(LoginFailedEvent e)
        {
            Debug.Log($"<color=red>✗ 登录失败: {e.Error}</color>");
        }

        private void OnDiamondChanged(DiamondChangedEvent e)
        {
            var changeType = e.Amount > 0 ? "<color=green>+" : "<color=orange>";
            Debug.Log($"钻石 {changeType}{e.Amount}</color> → <color=white>{e.Current}</color>");
        }

        private void OnDiamondChangeFailed(DiamondChangeFailedEvent e)
        {
            Debug.Log($"<color=red>✗ 钻石失败: {e.Reason}</color>");
        }

        private void OnGoldChanged(GoldChangedEvent e)
        {
            var changeType = e.Amount > 0 ? "<color=green>+" : "<color=orange>";
            Debug.Log($"金币 {changeType}{e.Amount}</color> → <color=white>{e.Current}</color>");
        }

        private void OnGoldChangeFailed(GoldChangeFailedEvent e)
        {
            Debug.Log($"<color=red>✗ 金币失败: {e.Reason}</color>");
        }

        [Button("查询背包", "背包")]
        private void TestGetInventory()
        {
            Debug.Log("<color=yellow>▶ 查询背包</color>");

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
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

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
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

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
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

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
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

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
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

            var loginSystem = this.GetSystem<LoginSystem>();
            if (!loginSystem.IsLoggedIn)
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

        private async UniTaskVoid GetResourcesAsync(ResourceService resourceService)
        {
            var (success, diamond, gold) = await resourceService.GetResourcesAsync();
            if (success)
            {
                Debug.Log($"<color=green>✓</color> 钻石:<color=cyan>{diamond}</color> 金币:<color=yellow>{gold}</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ 查询失败</color>");
            }
        }

        private void OnInventoryUpdated(InventoryUpdatedEvent e)
        {
            Debug.Log($"<color=green>✓ 背包更新</color> 物品数:{e.Inventory.items?.Length ?? 0} 格子:{e.Inventory.maxSlots}");
            if (e.Inventory.items != null)
            {
                foreach (var item in e.Inventory.items)
                {
                    var itemName = Configs.ItemConfig.GetName(item.itemId);
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
    }
}
