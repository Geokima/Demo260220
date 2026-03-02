using System;
using System.Collections.Generic;

namespace Framework.Modules.Config
{
    /// <summary>
    /// 配置系统接口
    /// </summary>
    public interface IConfigSystem : ISystem
    {
        /// <summary>
        /// 获取指定类型的配置表
        /// </summary>
        /// <typeparam name="TRow">配置行类型</typeparam>
        /// <returns>配置表单接口</returns>
        IConfigSheet<TRow> GetSheet<TRow>() where TRow : IConfigRow;

        /// <summary>
        /// 获取指定类型和 ID 的配置行
        /// </summary>
        /// <typeparam name="TRow">配置行类型</typeparam>
        /// <param name="id">唯一标识 ID</param>
        /// <returns>配置行对象，不存在则返回 null</returns>
        TRow Get<TRow>(int id) where TRow : class, IConfigRow;

        /// <summary>
        /// 扫描程序集中的所有配置行类型
        /// </summary>
        /// <returns>配置行类型列表</returns>
        List<Type> ScanConfigRowTypes();

        /// <summary>
        /// 根据配置行类型获取对应的资源文件名
        /// </summary>
        /// <param name="configRowType">配置行类型</param>
        /// <returns>资源文件名</returns>
        string GetConfigFileName(Type configRowType);

        /// <summary>
        /// 注册配置数据（JSON 格式）
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <param name="rowType">对应的配置行类型</param>
        void RegisterConfig(string json, Type rowType);
    }
}
