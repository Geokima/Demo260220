using Framework.Modules.Config;

namespace Game.Configs
{
    /// <summary>
    /// 物品配置
    /// </summary>
    public class ItemConfig : IConfigRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int MaxStack { get; set; }
        public int Value { get; set; }
        public string UseEffect { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Durability { get; set; }
        public string Rarity { get; set; }
        public int QuestId { get; set; }
        public bool IsKeyItem { get; set; }
    }
}
