using System;

namespace Game.Data
{
    /// <summary>
    /// 物品数据
    /// </summary>
    [Serializable]
    public class ItemData
    {
        /// <summary>物品唯一ID</summary>
        public string uid;
        /// <summary>物品配置ID</summary>
        public int itemId;
        /// <summary>数量</summary>
        public int count;
        /// <summary>是否绑定</summary>
        public bool bind;
    }
}
