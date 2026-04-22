using UnityEngine.UI;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.System;

public partial class UI_SelectScenePanel : UIPanel
{
    public Button Btn1;
    public Button Btn2;
    public Button Btn3;
    public Button Btn4;
    public Button Btn5;
    public Text TxtLabel;

    partial void InitComponents();

    void Awake()
    {
        Btn1 = transform.Find("Rect_List/Btn_1").GetComponent<Button>();
        Btn2 = transform.Find("Rect_List/Btn_2").GetComponent<Button>();
        Btn3 = transform.Find("Rect_List/Btn_3").GetComponent<Button>();
        Btn4 = transform.Find("Rect_List/Btn_4").GetComponent<Button>();
        Btn5 = transform.Find("Rect_List/Btn_5").GetComponent<Button>();
        TxtLabel = transform.Find("Txt_Label").GetComponent<Text>();
        InitComponents();
    }
}
