using Cysharp.Threading.Tasks;
using Framework;

namespace Game.Gameplay.CardBattle
{
    /// <summary> 最小不可分割的执行对象。通过 Superior Injection 模式与 QFramework 解耦，仅作为纯粹的逻辑数据承载体运行。 </summary>
    public interface IBattleAction
    {
        IActionQueueService Superior { get; set; }
        
        /// <summary> 异步执行逻辑，允许表现层等待动画播放完毕 </summary>
        UniTask ExecuteAsync();

        /// <summary> 对象池回收 </summary>
        void Recycle();
    }
}
