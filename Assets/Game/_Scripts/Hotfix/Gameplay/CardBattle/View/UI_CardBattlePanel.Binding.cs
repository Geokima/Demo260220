using UnityEngine;
using UnityEngine.UI;
using Framework.Modules.UI;

namespace Game.Gameplay.CardBattle
{
    public partial class UI_CardBattlePanel
    {
        [Header("Top Info")]
        public Text Text_TurnCount;
        public GameObject Obj_VisualLock;

        [Header("Player Stats")]
        public Text Text_PlayerHP;
        public Text Text_PlayerBlock;
        public Text Text_PlayerEnergy;
        
        [Header("Enemy Area")]
        public Transform EnemyContainer; // 挂载 HorizontalLayoutGroup

        [Header("Hand Area")]
        public Transform HandContainer; // 挂载卡牌的父物体
        public Button Btn_EndTurn;

        [Header("Piles")]
        public Text Text_DrawCount;
        public Text Text_DiscardCount;
    }
}
