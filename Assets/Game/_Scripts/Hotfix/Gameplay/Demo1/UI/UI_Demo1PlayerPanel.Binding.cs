using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.UI.Widget;

public partial class UI_Demo1PlayerPanel : UIPanel
{
    private Widget_PlayerStatusBar w_PlayerStatusBar;
    private Widget_CardBoard w_CardBoard;

    partial void InitComponents();

    void Awake()
    {
        w_PlayerStatusBar = transform.Find("PlayerStateBar").GetComponent<Widget_PlayerStatusBar>();
        w_CardBoard = transform.Find("CardBoard").GetComponent<Widget_CardBoard>();
        InitComponents();
    }
}
