using System.Collections.Generic;

namespace Game.Gameplay.Demo1
{
    /// <summary>
    /// 卡牌配置数据（模板）
    /// 用于 CardConfig 静态配置，不包含运行时状态
    /// </summary>
    public class CardData
    {
        public string Id;
        public string Name;
        public int Price;
        public int Size;
        public CardRank Rank;
        public CardType Type;
        public List<string> Tags = new List<string>();

        public int Damage;
        public int Shield;
        public int Poison;
        public int Cure;
        public int BulletCount;

        public float MaxCD;
    }

    /// <summary>
    /// 卡牌模板配置库 (模拟数据库)
    /// </summary>
    public static class CardConfig
    {
        /// <summary>
        /// 预定义的基础卡牌库
        /// </summary>
        public static readonly List<CardData> Templates = new List<CardData>
        {
            new CardData { 
                Id = "T1_Sword", Name = "锈迹铁剑", Price = 3, Size = 1, Rank = CardRank.Bronze, Type = CardType.Active, 
                Damage = 5, MaxCD = 3.0f, Tags = new List<string> { "Weapon" } 
            },
            new CardData { 
                Id = "T1_Shield", Name = "简陋木盾", Price = 4, Size = 1, Rank = CardRank.Bronze, Type = CardType.Active, 
                Shield = 4, MaxCD = 4.0f, Tags = new List<string> { "Shield" } 
            },
            new CardData { 
                Id = "T1_Poison", Name = "剧毒涂层", Price = 5, Size = 1, Rank = CardRank.Bronze, Type = CardType.Passive, 
                Poison = 2, Tags = new List<string> { "Poison", "Magic" } 
            },
            new CardData { 
                Id = "T1_Hammer", Name = "重型战锤", Price = 8, Size = 2, Rank = CardRank.Bronze, Type = CardType.Active, 
                Damage = 12, MaxCD = 6.0f, Tags = new List<string> { "Weapon" } 
            },
            new CardData { 
                Id = "T1_Dagger", Name = "刺客短匕", Price = 4, Size = 1, Rank = CardRank.Bronze, Type = CardType.Active, 
                Damage = 3, MaxCD = 1.5f, Tags = new List<string> { "Weapon" } 
            }
        };

        /// <summary>
        /// 根据模板创建运行时数据
        /// </summary>
        public static CardModel CreateInstance(string templateId)
        {
            var template = Templates.Find(t => t.Id == templateId);
            if (template == null) return null;

            return new CardModel(template);
        }
    }
}
