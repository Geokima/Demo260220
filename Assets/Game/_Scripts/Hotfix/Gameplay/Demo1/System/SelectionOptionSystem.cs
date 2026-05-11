using Framework;
using Framework.Modules.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Gameplay.Demo1.System
{
    public interface ISelectionOptionSystem : ISystem
    {
        RoundOption[] GetOptions();
    }

    public struct RoundOption
    {
        public GameState State;
        public string Name;
        public int Data;
    }

    public class SelectionOptionSystem : AbstractSystem, ISelectionOptionSystem
    {
        public RoundOption[] GetOptions()
        {
            var model = this.GetModel<Demo1Model>();
            int round = model.Round.Value;

            if (round == 3)
            {
                var enemyIds = PickDistinctEnemyIds(3);
                return enemyIds
                    .Select(id => new RoundOption { State = GameState.Battle, Name = "战斗", Data = id })
                    .ToArray();
            }

            if (round == 6)
            {
                var enemyIds = PickDistinctEnemyIds(1);
                int id = enemyIds.Count > 0 ? enemyIds[0] : 1;
                return new[]
                {
                    new RoundOption { State = GameState.Battle, Name = "战斗", Data = id }
                };
            }

            return new[]
            {
                new RoundOption { State = GameState.Shop, Name = "商店", Data = 0 },
                new RoundOption { State = GameState.Work, Name = "打工", Data = 0 },
                new RoundOption { State = GameState.Treasure, Name = "宝箱", Data = 0 }
            };
        }

        private List<int> PickDistinctEnemyIds(int count)
        {
            var configSystem = this.GetSystem<IConfigSystem>();
            var sheet = configSystem.GetSheet<Demo1EnemyConfig>();

            var ids = sheet.All()
                .Select(e => e.Id)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new List<int>();

            int pickCount = Mathf.Clamp(count, 0, ids.Count);
            for (int i = 0; i < pickCount; i++)
            {
                int j = UnityEngine.Random.Range(i, ids.Count);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }

            ids.RemoveRange(pickCount, ids.Count - pickCount);
            return ids;
        }
    }
}
