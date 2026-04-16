using UnityEngine;
using UnityEngine.EventSystems;
using Framework;

namespace Game.Gameplay.CardBattle
{
    /// <summary>
    /// 怪物/实体 UI 组件：负责处理卡牌指向时的目标锁定
    /// </summary>
    public class UI_EnemyItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IController
    {
        public IArchitecture Architecture { get; set; }
        
        [SerializeField] private EntityData _data;
        private BattleModel _model => Architecture?.GetModel<BattleModel>();

        public void SetData(EntityData data, IArchitecture arch)
        {
            _data = data;
            Architecture = arch;
            Debug.Log($"[EnemyUI] {gameObject.name} 初始化完成，绑定数据: {data.Name}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_model == null) return;
            
            // 如果当前玩家正在拖拽卡牌，则锁定目标为当前怪物
            if (_model.SelectedCard.Value != null)
            {
                _model.SelectedEnemy.Value = _data;
                Debug.Log($"[UI] 准星锁定目标: {_data.Name}");
                
                // 可以在这里增加怪物的 Scaling 或 Outline 强调
                transform.localScale = Vector3.one * 1.15f;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_model == null) return;

            // 划出时取消锁定
            if (_model.SelectedEnemy.Value == _data)
            {
                _model.SelectedEnemy.Value = null;
                transform.localScale = Vector3.one;
            }
        }
    }
}
