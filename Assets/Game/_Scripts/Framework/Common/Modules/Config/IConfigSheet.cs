using System;
using System.Collections.Generic;

namespace Framework.Modules.Config
{
    /// <summary>
    /// 配置表单接口
    /// </summary>
    /// <typeparam name="TRow">配置行类型</typeparam>
    public interface IConfigSheet<TRow> where TRow : IConfigRow
    {
        /// <summary>
        /// 获取行数
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 根据 ID 获取配置行
        /// </summary>
        /// <param name="id">唯一标识 ID</param>
        /// <returns>配置行对象，不存在则返回 null</returns>
        TRow Get(int id);

        /// <summary>
        /// 尝试根据 ID 获取配置行
        /// </summary>
        /// <param name="id">唯一标识 ID</param>
        /// <param name="row">输出的配置行对象</param>
        /// <returns>是否获取成功</returns>
        bool TryGet(int id, out TRow row);

        /// <summary>
        /// 根据字段选择器查找符合条件的第一个配置行
        /// </summary>
        /// <typeparam name="TField">字段类型</typeparam>
        /// <param name="selector">字段选择器</param>
        /// <param name="value">目标字段值</param>
        /// <returns>配置行对象，不存在则返回 null</returns>
        TRow FindBy<TField>(Func<TRow, TField> selector, TField value);

        /// <summary>
        /// 根据条件筛选配置行
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <returns>符合条件的配置行集合</returns>
        IEnumerable<TRow> Where(Func<TRow, bool> predicate);

        /// <summary>
        /// 将配置表转换为以指定字段为索引的字典
        /// </summary>
        /// <typeparam name="TField">索引字段类型</typeparam>
        /// <param name="keySelector">索引字段选择器</param>
        /// <returns>配置字典</returns>
        Dictionary<TField, TRow> ToIndex<TField>(Func<TRow, TField> keySelector);

        /// <summary>
        /// 获取所有配置行
        /// </summary>
        /// <returns>所有配置行的集合</returns>
        IEnumerable<TRow> All();
    }
}
