using UnityEngine;
using Framework.Modules.UI;
using DG.Tweening;
using Framework;
using Game.Gameplay.Demo1;

public partial class UI_BenchPanel : UIPanel
{
    partial void InitComponents()
    {
        // 在这里添加额外的组件初始化代码
    }

    override public void OnOpen(object data = null)
    {
        var model = this.GetModel<Demo1Model>().BenchCards;
        cardBoard.BindTo(model);
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.alpha = 0;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);
    }

    override public void OnClose()
    {
        cardBoard.Unbind();
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f);
    }
}
