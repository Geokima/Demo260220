using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;

public partial class UI_LoginPanel : UIPanel
{
    public Image ImgContent;
    public Image ImgTitleBar;
    public Text TxtTitle;
    public Text TxtTip;

    partial void InitComponents();

    protected override void Awake()
    {
        base.Awake();
        ImgContent = transform.Find("UI_LoginPanel/Img_Content").GetComponent<Image>();
        ImgTitleBar = transform.Find("UI_LoginPanel/Img_Content/Img_TitleBar").GetComponent<Image>();
        TxtTitle = transform.Find("UI_LoginPanel/Img_Content/Txt_Title").GetComponent<Text>();
        TxtTip = transform.Find("UI_LoginPanel/Img_Content/Txt_Tip").GetComponent<Text>();
        InitComponents();
    }
}
