using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.UI.Widget;

public partial class UI_ShopPanel : UIPanel
{
    public Widget_ShopZone ShopZone;
    public Text TxtLabel;
    public Button BtnQuit;
    public Button BtnRefresh;

    partial void InitComponents();

    void Awake()
    {
        ShopZone = transform.GetComponentInChildren<Widget_ShopZone>();
        TxtLabel = transform.Find("Txt_Label").GetComponent<Text>();
        BtnQuit = transform.Find("Btn_Quit").GetComponent<Button>();
        BtnRefresh = transform.Find("Btn_Refresh").GetComponent<Button>();
        InitComponents();
    }
}
