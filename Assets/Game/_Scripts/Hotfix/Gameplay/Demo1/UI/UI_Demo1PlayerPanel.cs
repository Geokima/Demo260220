using UnityEngine;
using Framework.Modules.UI;
using DG.Tweening;
using Framework;
using Game.Gameplay.Demo1;
using Game.Gameplay.Demo1.Event;
using Game.Gameplay.Demo1.Command;

public partial class UI_Demo1PlayerPanel : UIPanel
{
    private IUnregister _hpUnregister;
    private IUnregister _maxHpUnregister;
    private IUnregister _stateUnregister;
    private IUnregister _shieldUnregister;
    private IUnregister _poisonUnregister;

    partial void InitComponents()
    {
        btnBench.onClick.AddListener(() => this.SendCommand(new ToggleBenchPanelCommand()));
    }

    public override void OnInit()
    {
        var model = this.GetModel<Demo1Model>();
        w_CardBoard.BindTo(model.ActiveSlots);
    }

    override public void OnOpen(object data = null)
    {
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.alpha = 0;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);

        var model = this.GetModel<Demo1Model>();
        _hpUnregister = model.CurrentHP.RegisterWithInitValue(_ => UpdateHp());
        _maxHpUnregister = model.MaxHP.RegisterWithInitValue(_ => UpdateHp());
        _shieldUnregister = model.PlayerShield.RegisterWithInitValue(_ => UpdateShield());
        _poisonUnregister = model.PlayerPoison.RegisterWithInitValue(_ => UpdatePoison());
        _stateUnregister = this.RegisterEvent<GameStateChangedEvent>(e =>{ 
            w_CardBoard.SetDraggable(e.State != GameState.Battle);
            btnBench.gameObject.SetActive(e.State != GameState.Battle);
        });
    }

    override public void OnClose()
    {
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f);

        _hpUnregister?.Unregister();
        _maxHpUnregister?.Unregister();
        _shieldUnregister?.Unregister();
        _poisonUnregister?.Unregister();
        _stateUnregister?.Unregister();
    }

    private void UpdateHp()
    {
        if (w_HpBar == null)
            return;
        var model = this.GetModel<Demo1Model>();
        w_HpBar.SetHp(model.CurrentHP.Value, model.MaxHP.Value);
    }

    private void UpdateShield()
    {
        if (w_HpBar == null)
            return;
        var model = this.GetModel<Demo1Model>();
        w_HpBar.SetSheild(model.PlayerShield.Value);
    }

    private void UpdatePoison()
    {
        if (w_HpBar == null)
            return;
        var model = this.GetModel<Demo1Model>();
        w_HpBar.SetPoison(model.PlayerPoison.Value);
    }
}
