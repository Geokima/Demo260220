using UnityEngine;
using Framework.Modules.UI;
using DG.Tweening;

public partial class UI_SelectRewardPanel : UIPanel
{
    partial void InitComponents()
    {
        // 在这里添加额外的组件初始化代码
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
    }
}
