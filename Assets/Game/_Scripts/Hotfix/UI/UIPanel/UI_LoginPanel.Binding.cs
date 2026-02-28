using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;

public partial class UI_LoginPanel : UIPanel
{
    public InputField InputUserName;
    public InputField InputPassword;
    public Button BtnLogIn;

    partial void InitComponents();

    void Awake()
    {
        InputUserName = transform.Find("Img_Content/Input_UserName").GetComponent<InputField>();
        InputPassword = transform.Find("Img_Content/Input_Password").GetComponent<InputField>();
        BtnLogIn = transform.Find("Img_Content/Btn_LogIn").GetComponent<Button>();
        InitComponents();
    }
}
