using UnityEngine;

namespace Game.Gameplay.Demo1.UI
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class UI_SlotZoneBase : UIDropZoneBase
    {
        [SerializeField] protected int _capacity = 10;
        [SerializeField] protected float _cellWidth = 160f;
        [SerializeField] protected float _cellHeight = 220f;
        [SerializeField] protected float _spacing = 12f;
        [SerializeField] protected Vector2 _padding = new Vector2(16f, 16f);
        [SerializeField] private bool _drawSlotsGizmos = true;
        [SerializeField] private bool _autoResizeRect = true;

        protected RectTransform _rectTransform;

        protected virtual int MinCapacity => 1;
        protected virtual int MaxCapacity => 10;

        public int Capacity => _capacity;

        protected virtual void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ResizeRect();
        }

        protected void SetCapacityInternal(int capacity)
        {
            _capacity = Mathf.Clamp(capacity, MinCapacity, MaxCapacity);
            ResizeRect();
        }

        private void ResizeRect()
        {
            if (!_autoResizeRect)
                return;

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            float width = _padding.x * 2f + _capacity * _cellWidth + (_capacity - 1) * _spacing;
            float height = _padding.y * 2f + _cellHeight;
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        protected virtual bool TryGetOccupiedCells(out bool[] occupied)
        {
            occupied = null;
            return false;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (!_drawSlotsGizmos)
                return;

            var rt = GetComponent<RectTransform>();
            if (rt == null)
                return;

            int cap = Mathf.Clamp(_capacity, MinCapacity, MaxCapacity);
            bool[] occ = null;
            if (Application.isPlaying)
                TryGetOccupiedCells(out occ);

            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = rt.localToWorldMatrix;

            float unit = _cellWidth + _spacing;
            float originX = -rt.rect.width * rt.pivot.x;
            float centerY = -rt.rect.height * rt.pivot.y + _padding.y + _cellHeight * 0.5f;
            for (int i = 0; i < cap; i++)
            {
                float xMin = originX + _padding.x + i * unit;
                float centerX = xMin + _cellWidth * 0.5f;

                bool occupied = occ != null && i >= 0 && i < occ.Length && occ[i];
                Gizmos.color = occupied ? new Color(1f, 0.35f, 0.35f, 0.12f) : new Color(0.2f, 0.8f, 1f, 0.08f);
                Gizmos.DrawCube(new Vector3(centerX, centerY, 0f), new Vector3(_cellWidth, _cellHeight, 0.01f));

                Gizmos.color = occupied ? new Color(1f, 0.35f, 0.35f, 0.8f) : new Color(0.2f, 0.8f, 1f, 0.8f);
                Gizmos.DrawWireCube(new Vector3(centerX, centerY, 0f), new Vector3(_cellWidth, _cellHeight, 0.01f));
            }

            Gizmos.matrix = oldMatrix;
        }
    }
}
