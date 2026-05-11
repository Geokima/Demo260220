using UnityEngine;
using Framework.Modules.UI;
using DG.Tweening;
using Framework;
using Game.Gameplay.Demo1.System;
using Game.Gameplay.Demo1;
using Game.Gameplay.Demo1.Event;
using Game.Gameplay.Demo1.Command;

[GameState(GameState.Battle)]
public partial class UI_BattlePanel : UIPanel
{
    private IUnregister _enemyHpUnregister;
    private IUnregister _enemyMaxHpUnregister;
    private IUnregister _enemyShieldUnregister;
    private IUnregister _enemyPoisonUnregister;
    private IUnregister _battleEndedUnregister;

    partial void InitComponents()
    {
        BenchZone.SetDraggable(false);
        BtnQuit.onClick.AddListener(() =>
        {
            this.SendCommand(new QuitEncounterCommand());
        });
    }

    override public void OnOpen(object data = null)
    {
        TxtLabel.text = "战斗";
        BtnQuit.gameObject.SetActive(false);
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.alpha = 0;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);

        int enemyId = data is int id ? id : 1;
        var battleSystem = this.GetSystem<IBattleSystem>();
        battleSystem.StartBattle(enemyId);

        var model = this.GetModel<Demo1Model>();
        BenchZone.BindTo(model.EnemyCards);

        _enemyHpUnregister = model.EnemyHP.RegisterWithInitValue(_ => UpdateEnemyHp());
        _enemyMaxHpUnregister = model.EnemyMaxHP.RegisterWithInitValue(_ => UpdateEnemyHp());
        _enemyShieldUnregister = model.EnemyShield.RegisterWithInitValue(_ => UpdateEnemyShield());
        _enemyPoisonUnregister = model.EnemyPoison.RegisterWithInitValue(_ => UpdateEnemyPoison());
        _battleEndedUnregister = this.RegisterEvent<BattleEndedEvent>(OnBattleEnded);
    }

    override public void OnClose()
    {
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f);

        BenchZone.Unbind();
        _enemyHpUnregister?.Unregister();
        _enemyMaxHpUnregister?.Unregister();
        _enemyShieldUnregister?.Unregister();
        _enemyPoisonUnregister?.Unregister();
        _battleEndedUnregister?.Unregister();
    }

    private void OnBattleEnded(BattleEndedEvent e)
    {
        TxtLabel.text = e.PlayerWon ? "战斗胜利" : "战斗失败";
        BtnQuit.gameObject.SetActive(true);
    }

    private void UpdateEnemyHp()
    {
        var model = this.GetModel<Demo1Model>();
        if (HpBar != null)
            HpBar.SetHp(model.EnemyHP.Value, model.EnemyMaxHP.Value);
    }

    private void UpdateEnemyShield()
    {
        var model = this.GetModel<Demo1Model>();
        if (HpBar != null)
            HpBar.SetSheild(model.EnemyShield.Value);
    }

    private void UpdateEnemyPoison()
    {
        var model = this.GetModel<Demo1Model>();
        if (HpBar != null)
            HpBar.SetPoison(model.EnemyPoison.Value);
    }
}
