using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;

public partial class UI_LoginPanel : UIPanel
{
    public InputField InputUserName;
    public InputField InputPassword;
    public Button BtnLogin;

    partial void InitComponents();

    void Awake()
    {
        InputUserName = transform.Find("Img_Content/Input_UserName").GetComponent<InputField>();
        InputPassword = transform.Find("Img_Content/Input_Password").GetComponent<InputField>();
        BtnLogin = transform.Find("Img_Content/Btn_Login").GetComponent<Button>();
        InitComponents();
    }
}
