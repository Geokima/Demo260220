using Cysharp.Threading.Tasks;
using DG.Tweening;
using Framework;
using Framework.Modules.Scene;
using Framework.Modules.UI;
using UnityEngine;

public class UI_BlackScreen : UIPanel
{
    public override void OnInit()
    {
        this.RegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    
    public override void OnOpen(object data = null)
    {
        CanvasGroup.alpha = 0;
        CanvasGroup.blocksRaycasts = true;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);
    }

    public override void OnClose()
    {
        CanvasGroup.alpha = 1;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f).OnComplete(() =>
        {
            CanvasGroup.blocksRaycasts = false;
        });
    }

    private async void OnSceneLoadComplete(SceneLoadCompleteEvent e)
    {
        await UniTask.Delay(500);
        this.GetSystem<IUISystem>().Close<UI_BlackScreen>();
    }
}
