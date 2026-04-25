using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.UI.Widget;

public partial class UI_BenchPanel : UIPanel
{
    public Text TxtLabel;
    public Widget_CardBoard cardBoard;

    partial void InitComponents();

    void Awake()
    {
        TxtLabel = transform.Find("Txt_Label").GetComponent<Text>();
        cardBoard = transform.Find("BenchZone").GetComponent<Widget_CardBoard>();
        InitComponents();
    }
}
