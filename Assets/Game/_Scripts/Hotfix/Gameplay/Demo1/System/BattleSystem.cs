using Framework;
using Framework.Modules.Config;
using Game.Gameplay.Demo1.Event;
using UnityEngine;
using static Framework.Logger;

namespace Game.Gameplay.Demo1.System
{
    public interface IBattleSystem : ISystem
    {
        void StartBattle(int enemyId);
        void StopBattle();
        void UpdateBattle(float deltaTime);
        bool IsInBattle { get; }
    }

    public class BattleSystem : AbstractSystem, IBattleSystem
    {
        private Demo1Model _model;
        private float _accumulator = 0;
        private float _poisonAccumulator = 0;
        private const float TickInterval = 0.1f;

        public bool IsInBattle { get; private set; }

        public override void Init()
        {
            _model = this.GetModel<Demo1Model>();
        }

        public void StartBattle(int enemyId)
        {
            Log($"StartBattle with enemyId: {enemyId}");

            var configSystem = this.GetSystem<IConfigSystem>();
            var enemy = configSystem.GetSheet<Demo1EnemyConfig>().Get(enemyId);
            if (enemy == null)
                return;

            var cardDict = configSystem.GetSheet<Demo1CardConfig>().ToIndex(c => c.Id);

            _model.EnemyMaxHP.Value = enemy.MaxHP;
            _model.EnemyHP.Value = enemy.MaxHP;
            _model.EnemyShield.Value = 0;
            _model.EnemyPoison.Value = 0;

            _model.PlayerShield.Value = 0;
            _model.PlayerPoison.Value = 0;

            _model.EnemyCards.Clear();
            foreach (var cardId in enemy.CardIds)
            {
                if (cardDict.TryGetValue(cardId, out var config))
                {
                    var card = new CardModel(config);
                    card.ApplyPriceRule(isInShop: false);
                    card.CurrentCD.Value = config.MaxCD;
                    _model.EnemyCards.Add(card);
                }
            }

            IsInBattle = true;
            _accumulator = 0;
            _poisonAccumulator = 0;
        }

        public void StopBattle()
        {
            IsInBattle = false;
            this.SendEvent(new BattleEndedEvent { PlayerWon = _model.EnemyHP.Value <= 0 });

            ResetCards(_model.ActiveSlots);
            ResetCards(_model.EnemyCards);

            _model.PlayerShield.Value = 0;
            _model.PlayerPoison.Value = 0;
            _model.EnemyShield.Value = 0;
            _model.EnemyPoison.Value = 0;

            _model.CurrentHP.Value = _model.MaxHP.Value;
            _model.EnemyHP.Value = _model.EnemyMaxHP.Value;
        }

        private void ResetCards(BindableList<CardModel> cards)
        {
            foreach (var card in cards)
            {
                card?.ResetCD();
            }
        }

        public void UpdateBattle(float deltaTime)
        {
            if (!IsInBattle)
                return;

            _accumulator += deltaTime;

            while (_accumulator >= TickInterval)
            {
                _accumulator -= TickInterval;
                ProcessTick();
            }
        }

        private bool IsOver() => _model.EnemyHP.Value <= 0 || _model.CurrentHP.Value <= 0;

        private void ProcessTick()
        {
            if (IsOver())
            {
                StopBattle();
                return;
            }

            TickCD(_model.EnemyCards);
            TickCD(_model.ActiveSlots);

            _poisonAccumulator += TickInterval;
            while (_poisonAccumulator >= Demo1Const.PoisonTickInterval)
            {
                _poisonAccumulator -= Demo1Const.PoisonTickInterval;
                ApplyPoison();
            }

            ExecuteCards(_model.ActiveSlots, true);
            ExecuteCards(_model.EnemyCards, false);
            
        }

        private void TickCD(BindableList<CardModel> cards)
        {
            foreach (var card in cards)
            {
                if (card == null)
                    continue;
                if (card.CurrentCD.Value > 0)
                    card.CurrentCD.Value -= TickInterval;
            }
        }

        private void ApplyPoison()
        {
            if (_model.PlayerPoison.Value > 0)
                _model.CurrentHP.Value = Mathf.Max(0, _model.CurrentHP.Value - _model.PlayerPoison.Value);
            if (_model.EnemyPoison.Value > 0)
                _model.EnemyHP.Value = Mathf.Max(0, _model.EnemyHP.Value - _model.EnemyPoison.Value);
        }

        private void ExecuteCards(BindableList<CardModel> cards, bool isPlayer)
        {
            foreach (var card in cards)
            {
                if (card == null)
                    continue;
                if (card.Type == CardType.Passive)
                    continue;
                if (card.CurrentCD.Value > 0)
                    continue;

                int damage = card.Damage.Value;
                int shield = card.Shield.Value;
                int poison = card.Poison.Value;
                int cure = card.Cure.Value;

                if (isPlayer)
                {
                    if (damage > 0)
                        DealDamage(_model.EnemyShield, _model.EnemyHP, damage);
                    if (shield > 0)
                        _model.PlayerShield.Value += shield;
                    if (poison > 0)
                        _model.EnemyPoison.Value += poison;
                    if (cure > 0)
                        _model.CurrentHP.Value = Mathf.Min(_model.MaxHP.Value, _model.CurrentHP.Value + cure);
                }
                else
                {
                    if (damage > 0)
                        DealDamage(_model.PlayerShield, _model.CurrentHP, damage);
                    if (shield > 0)
                        _model.EnemyShield.Value += shield;
                    if (poison > 0)
                        _model.PlayerPoison.Value += poison;
                    if (cure > 0)
                        _model.EnemyHP.Value = Mathf.Min(_model.EnemyMaxHP.Value, _model.EnemyHP.Value + cure);
                }

                card.CurrentCD.Value = card.MaxCD.Value;
            }
        }

        private void DealDamage(BindableProperty<int> shield, BindableProperty<int> hp, int damage)
        {
            if (shield.Value > 0)
            {
                if (shield.Value >= damage)
                {
                    shield.Value -= damage;
                    return;
                }
                damage -= shield.Value;
                shield.Value = 0;
            }
            hp.Value = Mathf.Max(0, hp.Value - damage);
        }
    }
}
