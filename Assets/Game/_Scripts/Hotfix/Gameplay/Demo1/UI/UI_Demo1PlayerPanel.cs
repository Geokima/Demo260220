using UnityEngine;
using Framework.Modules.UI;
using DG.Tweening;
using Framework;
using Game.Gameplay.Demo1;

public partial class UI_Demo1PlayerPanel : UIPanel
{
    private IUnregister _hpUnregister;
    private IUnregister _maxHpUnregister;

    partial void InitComponents()
    {
    }

    public override void OnInit()
    {
        var model = this.GetModel<Demo1Model>();
        w_CardBoard.BindTo(model.ActiveSlots);
        _hpUnregister = model.CurrentHP.RegisterWithInitValue(_ => UpdateHp());
        _maxHpUnregister = model.MaxHP.RegisterWithInitValue(_ => UpdateHp());
    }

    override public void OnOpen(object data = null)
    {
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.alpha = 0;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);
    }

    override public void OnClose()
    {
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f);

        _hpUnregister?.Unregister();
        _maxHpUnregister?.Unregister();
    }

    private void UpdateHp()
    {
        if (w_HpBar == null)
            return;
        var model = this.GetModel<Demo1Model>();
        w_HpBar.SetHp(model.CurrentHP.Value, model.MaxHP.Value);
    }
}
