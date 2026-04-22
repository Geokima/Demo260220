using UnityEngine;
using Framework.Modules.UI;
using DG.Tweening;
using Framework;
using Game.Gameplay.Demo1;
using Game.Gameplay.Demo1.System;
using UnityEngine.UI;
using System;
using Game.Gameplay.Demo1.Command;

[GameState(GameState.Selection)]
public partial class UI_SelectScenePanel : UIPanel
{
    private Button[] btns;
    partial void InitComponents()
    {
        btns = new[]
        {
            Btn1,
            Btn2,
            Btn3,
            Btn4,
            Btn5
        };
    }

    override public void OnOpen(object data = null)
    {
        LoadSelectOptions();
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

    private void LoadSelectOptions()
    {
        var options = this.GetSystem<ISelectionOptionSystem>().GetOptions();
        for (int i = 0; i < 5; i++)
        {
            if(i < options.Length)
            {
                var option = options[i];

                SetButtonText(btns[i], option.Name, () =>
                {
                    this.SendCommand(new ChangeGameStateCommand { State = option.State, Data = option.Data });
                });
            }
            else
            {
                SetButtonText(btns[i], "");
            }
        }
    }

    private void SetButtonText(Button btn, string text, Action onClick = null)
    {
        btn.GetComponentInChildren<Text>().text = text;
        if(text == "")
            btn.gameObject.SetActive(false);
        else
            btn.gameObject.SetActive(true);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }
}
