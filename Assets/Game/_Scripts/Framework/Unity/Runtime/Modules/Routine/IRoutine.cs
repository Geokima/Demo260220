using Cysharp.Threading.Tasks;

namespace Framework.Modules.Routine
{
    /// <summary>
    /// 异步流程单元接口 - 任何可以被"运行并等待完成"的东西
    /// 例如：动画、音效、延时、逻辑等待，均可实现此接口并加入序列
    /// </summary>
    public interface IRoutine
    {
        UniTask RunAsync();
    }
}
