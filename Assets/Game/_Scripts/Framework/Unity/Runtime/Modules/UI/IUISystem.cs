using System;
using Framework;
using UnityEngine;

namespace Framework.Modules.UI
{
    /// <summary>
    /// UI 系统接口
    /// </summary>
    public interface IUISystem : ISystem
    {
        /// <summary>
        /// UI 根画布
        /// </summary>
        Canvas CanvasRoot { get; }

        /// <summary>
        /// UI 根节点 RectTransform
        /// </summary>
        RectTransform CanvasRootRect { get; }

        /// <summary>
        /// 当前导航栈中的面板数量
        /// </summary>
        int NavigationStackCount { get; }

        /// <summary>
        /// 获取已打开的面板实例
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <returns>面板实例，未打开则返回 null</returns>
        T GetPanel<T>() where T : UIPanel;

        /// <summary>
        /// 检查面板是否已打开
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <returns>是否已打开</returns>
        bool IsOpen<T>() where T : UIPanel;

        /// <summary>
        /// 打开面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="data">传递给面板的初始化数据</param>
        void Open<T>(object data = null) where T : UIPanel;

        /// <summary>
        /// 关闭面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        void Close<T>() where T : UIPanel;

        /// <summary>
        /// 关闭指定层级的所有面板
        /// </summary>
        /// <param name="layers">目标层级列表</param>
        void CloseAll(params UILayer[] layers);
    }
}
