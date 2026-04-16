using System;
using Framework;
using Game.Gameplay.Demo1.UI.Widget;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Gameplay.Demo1.UI
{
    public class UI_CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Widget_CardValueLabel DamageLabel;
        public Widget_CardValueLabel ShieldLabel;
        public Widget_CardValueLabel CureLabel;
        public Widget_CardValueLabel PoisonLabel;
        public Widget_CardValueLabel BulletCountLabel;
        public Widget_CardValueLabel CDLabel;
        public Image CDProgressImage;
        public Text PriceText;
        public Text Rank;
        public Text Name;

        private CardModel _model;

        private IUnregister _damageUnregister;
        private IUnregister _shieldUnregister;
        private IUnregister _cureUnregister;
        private IUnregister _poisonUnregister;
        private IUnregister _bulletUnregister;
        private IUnregister _priceUnregister;
        private IUnregister _rankUnregister;
        private IUnregister _cdUnregister;
        private IUnregister _maxCDUnregister;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        public bool Draggable { get; set; } = true;
        public UIDropZoneBase OwnerZone { get; internal set; }
        public int WidthInCells { get; private set; } = 1;
        public RectTransform RectTransform => _rectTransform != null ? _rectTransform : (_rectTransform = GetComponent<RectTransform>());
        public CanvasGroup CanvasGroup => _canvasGroup != null ? _canvasGroup : (_canvasGroup = GetComponent<CanvasGroup>());
        public Action<UI_CardView> Clicked;

        public CardModel Model
        {
            get => _model;
            set
            {
                UnbindAll();
                _model = value;
                if (_model != null)
                    BindAll();
            }
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnDestroy()
        {
            UnbindAll();
        }

        private void UpdateCDProgress()
        {
            float progress = 1f - (_model.CurrentCD.Value / _model.MaxCD.Value);
            CDProgressImage.fillAmount = progress;
        }

        private void BindAll()
        {
            _damageUnregister = _model.Damage.RegisterWithInitValue(v => DamageLabel.SetText(v.ToString()));
            _shieldUnregister = _model.Shield.RegisterWithInitValue(v => ShieldLabel.SetText(v.ToString()));
            _cureUnregister = _model.Cure.RegisterWithInitValue(v => CureLabel.SetText(v.ToString()));
            _poisonUnregister = _model.Poison.RegisterWithInitValue(v => PoisonLabel.SetText(v.ToString()));
            _bulletUnregister = _model.BulletCount.RegisterWithInitValue(v => BulletCountLabel.SetText(v.ToString()));
            _maxCDUnregister = _model.MaxCD.RegisterWithInitValue(v => CDLabel.SetText(v.ToString("F1")));
            _cdUnregister = _model.CurrentCD.RegisterWithInitValue(v => UpdateCDProgress());
            _priceUnregister = _model.Price.RegisterWithInitValue(v => PriceText.text = $"${v}");
            _rankUnregister = _model.Rank.RegisterWithInitValue(v => Rank.text = v.ToString());

            Name.text = _model.Name;

            SetWidthInCells(_model.Size);
        }

        private void UnbindAll()
        {
            _damageUnregister?.Unregister();
            _shieldUnregister?.Unregister();
            _cureUnregister?.Unregister();
            _poisonUnregister?.Unregister();
            _bulletUnregister?.Unregister();
            _priceUnregister?.Unregister();
            _rankUnregister?.Unregister();
            _cdUnregister?.Unregister();
            _maxCDUnregister?.Unregister();
        }

        public void SetWidthInCells(int widthInCells)
        {
            WidthInCells = Mathf.Clamp(widthInCells, 1, 3);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Draggable)
                return;
            UIDragManager.Instance.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Draggable)
                return;
            UIDragManager.Instance.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Draggable)
                return;
            UIDragManager.Instance.EndDrag(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
        }
    }
}
