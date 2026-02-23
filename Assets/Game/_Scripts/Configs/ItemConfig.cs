using Framework;
using Framework.Modules.Config;

namespace Game.Configs
{
    /// <summary>
    /// 物品配置查询工具
    /// </summary>
    public static class ItemConfig
    {
        public static ItemConfigRow Get(int itemId)
        {
            return GameArchitecture.Instance.GetSystem<ConfigSystem>().Get<ItemConfigRow>(itemId);
        }

        public static string GetName(int itemId)
        {
            return Get(itemId)?.Name ?? $"未知物品({itemId})";
        }

        public static string GetIcon(int itemId)
        {
            return Get(itemId)?.Icon;
        }

        public static string GetDesc(int itemId)
        {
            return Get(itemId)?.Description;
        }

        public static int GetMaxStack(int itemId)
        {
            return Get(itemId)?.MaxStack ?? 99;
        }

        public static bool IsConsumable(int itemId)
        {
            return Get(itemId)?.Type == "Consumable";
        }
    }
}
