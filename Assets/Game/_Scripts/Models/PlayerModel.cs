using Framework;

namespace Game.Models
{
    /// <summary>
    /// 玩家数据模型 - 包含货币、等级、经验、体力等基础属性
    /// </summary>
    public class PlayerModel : AbstractModel
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        public enum ResourceType
        {
            Gold,       // 金币
            Diamond,    // 钻石
            Exp,        // 经验
            Energy,     // 体力
        }

        /// <summary>金币</summary>
        public BindableProperty<long> Gold { get; } = new BindableProperty<long>(0);
        /// <summary>钻石</summary>
        public BindableProperty<long> Diamond { get; } = new BindableProperty<long>(0);
        /// <summary>经验</summary>
        public BindableProperty<long> Exp { get; } = new BindableProperty<long>(0);
        /// <summary>体力</summary>
        public BindableProperty<long> Energy { get; } = new BindableProperty<long>(100);

        /// <summary>
        /// 获取资源
        /// </summary>
        public long GetResource(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => Gold.Value,
                ResourceType.Diamond => Diamond.Value,
                ResourceType.Exp => Exp.Value,
                ResourceType.Energy => Energy.Value,
                _ => 0
            };
        }

        /// <summary>
        /// 设置资源
        /// </summary>
        public void SetResource(ResourceType type, long amount)
        {
            switch (type)
            {
                case ResourceType.Gold: Gold.Value = amount; break;
                case ResourceType.Diamond: Diamond.Value = amount; break;
                case ResourceType.Exp: Exp.Value = amount; break;
                case ResourceType.Energy: Energy.Value = amount; break;
            }
        }

        /// <summary>
        /// 增加资源
        /// </summary>
        public void AddResource(ResourceType type, long amount)
        {
            if (amount <= 0) return;
            SetResource(type, GetResource(type) + amount);
        }

        /// <summary>
        /// 消耗资源
        /// </summary>
        public bool ConsumeResource(ResourceType type, long amount)
        {
            if (amount <= 0) return true;
            var current = GetResource(type);
            if (current < amount) return false;
            SetResource(type, current - amount);
            return true;
        }

        /// <summary>
        /// 检查资源是否足够
        /// </summary>
        public bool HasEnough(ResourceType type, long amount)
        {
            return GetResource(type) >= amount;
        }

        public override void Init()
        {
            // BindableProperty自动初始化
        }

        public override void Deinit()
        {
            Gold.Value = 0;
            Diamond.Value = 0;
            Exp.Value = 0;
            Energy.Value = 100;
        }
    }
}
