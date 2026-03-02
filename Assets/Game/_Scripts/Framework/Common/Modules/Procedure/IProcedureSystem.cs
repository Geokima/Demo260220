using System;
using System.Collections.Generic;
using Framework;
using Framework.Modules.FSM;

namespace Framework.Modules.Procedure
{
    /// <summary>
    /// 流程系统接口
    /// </summary>
    public interface IProcedureSystem : ISystem
    {
        /// <summary>
        /// 当前流程类型
        /// </summary>
        Type CurrentProcedure { get; }

        /// <summary>
        /// 流程变更事件：参数1为原流程类型，参数2为新流程类型
        /// </summary>
        event Action<Type, Type> OnProcedureChanged;

        /// <summary>
        /// 检查是否处于指定流程
        /// </summary>
        /// <typeparam name="T">流程类型</typeparam>
        /// <returns>是否处于该流程</returns>
        bool IsInProcedure<T>() where T : IProcedure;

        /// <summary>
        /// 注册流程
        /// </summary>
        /// <param name="procedure">流程实例</param>
        void RegisterProcedure(IProcedure procedure);

        /// <summary>
        /// 注册流程转换条件
        /// </summary>
        /// <typeparam name="TFrom">起始流程类型</typeparam>
        /// <typeparam name="TTo">目标流程类型</typeparam>
        /// <param name="condition">转换条件</param>
        void RegisterTransitionCondition<TFrom, TTo>(ITransitionCondition<Type> condition)
            where TFrom : IProcedure
            where TTo : IProcedure;

        /// <summary>
        /// 启动流程系统
        /// </summary>
        /// <typeparam name="T">初始流程类型</typeparam>
        void Start<T>() where T : IProcedure;

        /// <summary>
        /// 切换流程
        /// </summary>
        /// <typeparam name="T">目标流程类型</typeparam>
        void ChangeProcedure<T>() where T : IProcedure;

        /// <summary>
        /// 尝试切换流程
        /// </summary>
        /// <typeparam name="T">目标流程类型</typeparam>
        /// <returns>是否切换成功</returns>
        bool TryChangeProcedure<T>() where T : IProcedure;
    }
}
