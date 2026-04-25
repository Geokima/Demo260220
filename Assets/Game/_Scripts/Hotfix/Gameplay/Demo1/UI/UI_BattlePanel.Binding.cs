using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.UI.Widget;

public partial class UI_BattlePanel : UIPanel
{
    public Widget_CardBoard BenchZone;
    public Widget_HpBar HpBar;
    public Text TxtLabel;
    public Button BtnQuit;

    partial void InitComponents();

    void Awake()
    {
        BenchZone = transform.Find("CardBoard").GetComponent<Widget_CardBoard>();
        HpBar = transform.Find("HPBar").GetComponent<Widget_HpBar>();
        TxtLabel = transform.Find("Txt_Label").GetComponent<Text>();
        BtnQuit = transform.Find("Btn_Quit").GetComponent<Button>();
        InitComponents();
    }
}
