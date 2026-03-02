using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Framework;
using Framework.Modules.Scene;
using Framework.Modules.UI;
using UnityEngine;

public class UI_BlackScreen : UIPanel
{
    public override void OnOpen(object data = null)
    {
        CanvasGroup.alpha = 0;
        CanvasGroup.blocksRaycasts = true;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);
        this.RegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
    }

    public override void OnClose()
    {
        CanvasGroup.alpha = 1;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f).OnComplete(() =>
        {
            CanvasGroup.blocksRaycasts = false;
            this.UnRegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        });
    }

    private void OnSceneLoadComplete(SceneLoadCompleteEvent e)
    {
        this.GetSystem<IUISystem>().Close<UI_BlackScreen>();
    }

    private void OnDestroy()
    {
        this.UnRegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
    }
}
