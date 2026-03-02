using System;
using System.Collections.Generic;
using Framework;

namespace Framework.Modules.Pool
{
    /// <summary>
    /// 对象池系统接口
    /// </summary>
    public interface IPoolSystem : ISystem
    {
        /// <summary>
        /// 获取或创建一个简单对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="onReset">对象回收时的重置动作</param>
        /// <returns>简单对象池实例</returns>
        SimpleObjectPool<T> GetPool<T>(Action<T> onReset = null) where T : new();

        /// <summary>
        /// 获取所有对象池的状态信息
        /// </summary>
        /// <returns>状态字典：键为池名，值为 (数量, 分类)</returns>
        Dictionary<string, (int count, string category)> GetPoolStats();
    }
}
