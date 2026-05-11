using Cysharp.Threading.Tasks;
using DG.Tweening;
using Framework;
using Framework.Modules.Scene;
using Framework.Modules.UI;
using UnityEngine;
using UnityEngine.UI;

public class UI_BlackScreen : UIPanel
{
    public const float DefaultFadeDuration = 1.0f;
    public Image Image;
    public override void OnInit()
    {
        Image = transform.GetComponentInChildren<Image>();
        this.RegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    
    public override void OnOpen(object data = null)
    {
        CanvasGroup.alpha = 0;
        CanvasGroup.blocksRaycasts = true;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, DefaultFadeDuration);
    }

    public override void OnClose()
    {
        CanvasGroup.alpha = 1;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, DefaultFadeDuration).OnComplete(() =>
        {
            CanvasGroup.blocksRaycasts = false;
            SetColor(Color.black);
        });
    }
    
    public void SetColor(Color color)
    {
        if (Image)
            Image.color = color;
    }

    private async void OnSceneLoadComplete(SceneLoadCompleteEvent e)
    {
        await UniTask.Delay(500);
        this.GetSystem<IUISystem>().Close<UI_BlackScreen>();
    }
}
