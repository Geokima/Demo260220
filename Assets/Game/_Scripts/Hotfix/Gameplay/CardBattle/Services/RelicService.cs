using Framework;
using System.Collections.Generic;

namespace Game.Gameplay.CardBattle
{
    public interface IRelicService : ISystem
    {
        void AddRelic(IBattleRelic relic);
        void RemoveRelic(IBattleRelic relic);
        void Clear();
    }

    public class RelicService : AbstractSystem, IRelicService
    {
        private readonly List<IBattleRelic> _relics = new List<IBattleRelic>();

        public override void Init() { }

        public void AddRelic(IBattleRelic relic)
        {
            if (!_relics.Contains(relic))
            {
                relic.Init(this);
                _relics.Add(relic);
            }
        }

        public void RemoveRelic(IBattleRelic relic)
        {
            if (_relics.Contains(relic))
            {
                relic.Deinit();
                _relics.Remove(relic);
            }
        }

        public void Clear()
        {
            foreach (var relic in _relics)
                relic.Deinit();
            _relics.Clear();
        }
    }

    public interface IBattleRelic
    {
        void Init(ISystem service);
        void Deinit();
    }
}
