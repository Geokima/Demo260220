using UnityEngine;
using Framework;
using Framework.Modules.UI;
using Game.Auth;
using DG.Tweening;
using Game.Scene;

public partial class UI_LoginPanel : UIPanel
{
    partial void InitComponents()
    {
        BtnLogin.onClick.AddListener(OnLoginButtonClick);
    }

    override public void OnOpen(object data = null)
    {
        InputUserName.text = "";
        InputPassword.text = "";
        this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
        this.RegisterEvent<LoginFailedEvent>(OnLoginFailed);
        CanvasGroup.alpha = 0;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);
    }

    override public void OnClose()
    {
        this.UnregisterEvent<LoginSuccessEvent>(OnLoginSuccess);
        this.UnregisterEvent<LoginFailedEvent>(OnLoginFailed);
        CanvasGroup.alpha = 1;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f);
    }

    void OnDestroy()
    {
        BtnLogin.onClick.RemoveListener(OnLoginButtonClick);
        this.UnregisterEvent<LoginSuccessEvent>(OnLoginSuccess);
        this.UnregisterEvent<LoginFailedEvent>(OnLoginFailed);
    }

    private void OnLoginFailed(LoginFailedEvent @event)
    {
        Debug.LogError($"Login failed: {@event.Error}");
    }

    private void OnLoginSuccess(LoginSuccessEvent @event)
    {
        Debug.Log($"Login success: {@event.UserId} {@event.Token}");
        this.GetSystem<IUISystem>().Close<UI_LoginPanel>();
        this.SendCommand(new ChangeSceneCommand { SceneGroup = "Main" });
    }

    private void OnLoginButtonClick()
    {
        this.SendCommand(new LoginCommand { Username = InputUserName.text, Password = InputPassword.text });
    }
}
