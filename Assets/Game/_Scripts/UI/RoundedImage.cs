using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 圆角矩形 Image
    /// 支持四个角分别设置不同圆角半径
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteInEditMode]
    public class RoundedImage : MonoBehaviour
    {
        [Header("圆角半径（0-0.5，相对于短边的比例）")]
        [Range(0, 0.5f)]
        [SerializeField] private float _topLeftRadius = 0.1f;
        [Range(0, 0.5f)]
        [SerializeField] private float _topRightRadius = 0.1f;
        [Range(0, 0.5f)]
        [SerializeField] private float _bottomRightRadius = 0.1f;
        [Range(0, 0.5f)]
        [SerializeField] private float _bottomLeftRadius = 0.1f;

        private Image _image;
        private Material _material;
        private RectTransform _rectTransform;
        private Vector2 _lastSize;

        private static readonly int RadiusTLProperty = Shader.PropertyToID("_RadiusTL");
        private static readonly int RadiusTRProperty = Shader.PropertyToID("_RadiusTR");
        private static readonly int RadiusBRProperty = Shader.PropertyToID("_RadiusBR");
        private static readonly int RadiusBLProperty = Shader.PropertyToID("_RadiusBL");
        private static readonly int AspectRatioProperty = Shader.PropertyToID("_AspectRatio");

        public float TopLeftRadius
        {
            get => _topLeftRadius;
            set
            {
                _topLeftRadius = Mathf.Clamp(value, 0, 0.5f);
                UpdateMaterial();
            }
        }

        public float TopRightRadius
        {
            get => _topRightRadius;
            set
            {
                _topRightRadius = Mathf.Clamp(value, 0, 0.5f);
                UpdateMaterial();
            }
        }

        public float BottomRightRadius
        {
            get => _bottomRightRadius;
            set
            {
                _bottomRightRadius = Mathf.Clamp(value, 0, 0.5f);
                UpdateMaterial();
            }
        }

        public float BottomLeftRadius
        {
            get => _bottomLeftRadius;
            set
            {
                _bottomLeftRadius = Mathf.Clamp(value, 0, 0.5f);
                UpdateMaterial();
            }
        }

        /// <summary>
        /// 统一设置四个角的圆角半径
        /// </summary>
        public void SetRadius(float radius)
        {
            radius = Mathf.Clamp(radius, 0, 0.5f);
            _topLeftRadius = radius;
            _topRightRadius = radius;
            _bottomRightRadius = radius;
            _bottomLeftRadius = radius;
            UpdateMaterial();
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            SetupMaterial();
        }

        private void OnEnable()
        {
            SetupMaterial();
        }

        private void OnValidate()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            SetupMaterial();
        }

        private void SetupMaterial()
        {
            if (_image == null) return;

            if (_material == null)
            {
                Shader shader = Shader.Find("UI/RoundedImage");
                if (shader == null)
                {
                    Debug.LogError("[RoundedImage] 找不到 Shader: UI/RoundedImage");
                    return;
                }
                _material = new Material(shader);
            }

            _image.material = _material;
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (_material == null) return;

            _material.SetFloat(RadiusTLProperty, _topLeftRadius);
            _material.SetFloat(RadiusTRProperty, _topRightRadius);
            _material.SetFloat(RadiusBRProperty, _bottomRightRadius);
            _material.SetFloat(RadiusBLProperty, _bottomLeftRadius);

            UpdateAspectRatio();
        }

        private void UpdateAspectRatio()
        {
            if (_material == null || _rectTransform == null) return;

            Vector2 size = _rectTransform.rect.size;
            float aspectRatio = size.y / size.x; // 高/宽
            _material.SetFloat(AspectRatioProperty, aspectRatio);
        }

        private void Update()
        {
            // 只在尺寸变化时更新宽高比
            if (_rectTransform != null)
            {
                Vector2 currentSize = _rectTransform.rect.size;
                if (currentSize != _lastSize)
                {
                    _lastSize = currentSize;
                    UpdateAspectRatio();
                }
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
