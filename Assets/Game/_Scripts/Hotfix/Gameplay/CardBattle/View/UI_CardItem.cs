using UnityEngine;
using UnityEngine.UI;
using Framework;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace Game.Gameplay.CardBattle
{
    /// <summary>
    /// 单张卡牌的表现层脚本 - 依赖注入版
    /// </summary>
    public class UI_CardItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IController
    {
        public CardData Data { get; private set; }
        
        [SerializeField] private Text Text_Name;
        [SerializeField] private Text Text_Desc;
        [SerializeField] private Text Text_Cost;
        [SerializeField] private Image Img_Bg;

        private Vector3 _originalPos;
        private CanvasGroup _canvasGroup;
        private CanvasGroup CanvasGroup 
        {
            get 
            {
                if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        // QFramework IController 接口要求的属性 (由上级注入)
        public IArchitecture Architecture { get; set; }

        // 所有的 Model/System 获取都通过注入的 Architecture
        private BattleModel _model => Architecture?.GetModel<BattleModel>();

        private void Awake()
        {
            // 预初始化
            var cg = CanvasGroup;
        }

        public void SetData(CardData data)
        {
            Data = data;
            if (Text_Name != null) Text_Name.text = data.Name;
            
            // 使用 QFramework 的 RegisterWithInitValue 响应式更新
            data.Description.RegisterWithInitValue(desc => {
                if (Text_Desc != null) Text_Desc.text = desc;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            data.CurrentCost.RegisterWithInitValue(cost => {
                if (Text_Cost != null) Text_Cost.text = cost.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            _originalPos = transform.localPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Architecture == null || _model == null || Data == null) return;
            if (_model.VisualLockCount.Value > 0) return;
            if (_model.Player.Energy.Value < Data.CurrentCost.Value) return;

            _model.SelectedCard.Value = Data;
            CanvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_model.SelectedCard.Value != Data) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_model.SelectedCard.Value != Data) return;

            var enemy = _model.SelectedEnemy.Value;
            if (CanPlay(enemy))
            {
                this.SendCommand(new PlayCardCommand { Card = Data, Target = enemy });
            }

            ResetVisual();
            _model.SelectedCard.Value = null;
        }

        private bool CanPlay(EntityData target)
        {
            if (Data.TargetType == CardTargetType.SingleEnemy && target == null) return false;
            if (_model.Player.Energy.Value < Data.CurrentCost.Value) return false;
            return true;
        }

        private void ResetVisual()
        {
            CanvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;
            transform.localPosition = _originalPos;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Architecture == null || _model.SelectedCard.Value != null || _model.VisualLockCount.Value > 0) return;
            
            transform.localScale = Vector3.one * 1.2f;
            transform.localPosition = _originalPos + Vector3.up * 20f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Architecture == null || _model.SelectedCard.Value != null) return;
            
            transform.localScale = Vector3.one;
            transform.localPosition = _originalPos;
        }
    }
}
