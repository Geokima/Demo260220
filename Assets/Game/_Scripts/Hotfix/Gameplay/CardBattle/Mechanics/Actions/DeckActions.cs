using Framework;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay.CardBattle
{
    public class DrawCardAction : IPoolableAction
    {
        public bool IsCompleted { get; private set; } = false;
        public IActionQueueService Superior { get; set; }

        private int _amount;

        public DrawCardAction Init(int amount)
        {
            _amount = amount;
            IsCompleted = false;
            return this;
        }

        public void Reset()
        {
            _amount = 0;
            IsCompleted = false;
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            var model = Superior.GetModel<BattleModel>();

            for (int i = 0; i < _amount; i++)
            {
                if (model.DrawPile.Count == 0)
                {
                    if (model.DiscardPile.Count == 0) break;
                    
                    // BindableList 不支持 AddRange，需循环添加
                    foreach (var card in model.DiscardPile)
                    {
                        model.DrawPile.Add(card);
                    }
                    model.DiscardPile.Clear();
                }

                if (model.DrawPile.Count > 0)
                {
                    var card = model.DrawPile[0];
                    model.DrawPile.RemoveAt(0);

                    if (model.Hand.Count < 10)
                        model.Hand.Add(card);
                    else
                        model.DiscardPile.Add(card);
                }
            }

            await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);
            IsCompleted = true;
        }

        public void Recycle() => ActionPool<DrawCardAction>.Recycle(this);
    }

    public class DiscardHandAction : IPoolableAction
    {
        public bool IsCompleted { get; private set; } = false;
        public IActionQueueService Superior { get; set; }

        public void Reset()
        {
            IsCompleted = false;
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            var model = Superior.GetModel<BattleModel>();

            foreach (var card in model.Hand)
                model.DiscardPile.Add(card);

            model.Hand.Clear();

            await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);
            IsCompleted = true;
        }

        public void Recycle() => ActionPool<DiscardHandAction>.Recycle(this);
    }
}
