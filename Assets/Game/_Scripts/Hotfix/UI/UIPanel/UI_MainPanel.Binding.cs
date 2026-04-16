using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;

public partial class UI_MainPanel : UIPanel
{
    public RawImage RawImgBackground;
    public Image ImgContent;
    public Button BtnTest01;
    public Button BtnTest02;
    public Button BtnQuit;

    partial void InitComponents();

    void Awake()
    {
        RawImgBackground = transform.Find("RawImg_Background").GetComponent<RawImage>();
        ImgContent = transform.Find("Img_Content").GetComponent<Image>();
        BtnTest01 = transform.Find("Img_Content/Btn_Test01").GetComponent<Button>();
        BtnTest02 = transform.Find("Img_Content/Btn_Test02").GetComponent<Button>();
        BtnQuit = transform.Find("Img_Content/Btn_Quit").GetComponent<Button>();
        InitComponents();
    }
}
