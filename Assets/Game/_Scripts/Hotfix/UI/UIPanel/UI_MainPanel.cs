using UnityEngine;
using Framework.Modules.UI;
using DG.Tweening;
using Framework;
using Framework.Modules.Procedure;
using Game.Procedures;

public partial class UI_MainPanel : UIPanel
{
    partial void InitComponents()
    {
        BtnQuit.onClick.AddListener(() =>
        {
            if(Application.isEditor)
                UnityEditor.EditorApplication.isPlaying = false;
            else
                Application.Quit();
        });

        BtnTest01.onClick.AddListener(() =>
        {
            this.GetSystem<IProcedureSystem>().ChangeProcedure<Demo1Procedure>();
            this.GetSystem<IUISystem>().Close<UI_MainPanel>();
        });
        //BtnTest02.onClick.AddListener(() => this.GetSystem<IUISystem>().Close<UI_MainPanel>());
    }

    public override void OnOpen(object data = null)
    {
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.alpha = 0;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);
    }

    public override void OnClose()
    {
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        CanvasGroup.alpha = 1;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f);
    }
}