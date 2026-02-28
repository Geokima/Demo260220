using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(Image))]
    [ExecuteInEditMode]
    public class RoundedImageEffect : MonoBehaviour
    {
        #region Constants
        private const float MaxRadius = 0.5f;

        private static readonly int RadiusTLProperty = Shader.PropertyToID("_RadiusTL");
        private static readonly int RadiusTRProperty = Shader.PropertyToID("_RadiusTR");
        private static readonly int RadiusBRProperty = Shader.PropertyToID("_RadiusBR");
        private static readonly int RadiusBLProperty = Shader.PropertyToID("_RadiusBL");
        private static readonly int AspectRatioProperty = Shader.PropertyToID("_AspectRatio");
        #endregion

        #region SerializeField
        [Range(0, MaxRadius)]
        [SerializeField] private float _topLeftRadius = 0.1f;
        [Range(0, MaxRadius)]
        [SerializeField] private float _topRightRadius = 0.1f;
        [Range(0, MaxRadius)]
        [SerializeField] private float _bottomRightRadius = 0.1f;
        [Range(0, MaxRadius)]
        [SerializeField] private float _bottomLeftRadius = 0.1f;
        #endregion

        #region Private Fields
        private Image _image;
        private Material _material;
        private RectTransform _rectTransform;
        private Vector2 _lastSize;
        #endregion

        #region Properties
        public float TopLeftRadius
        {
            get => _topLeftRadius;
            set
            {
                _topLeftRadius = Mathf.Clamp(value, 0, MaxRadius);
                UpdateMaterial();
            }
        }

        public float TopRightRadius
        {
            get => _topRightRadius;
            set
            {
                _topRightRadius = Mathf.Clamp(value, 0, MaxRadius);
                UpdateMaterial();
            }
        }

        public float BottomRightRadius
        {
            get => _bottomRightRadius;
            set
            {
                _bottomRightRadius = Mathf.Clamp(value, 0, MaxRadius);
                UpdateMaterial();
            }
        }

        public float BottomLeftRadius
        {
            get => _bottomLeftRadius;
            set
            {
                _bottomLeftRadius = Mathf.Clamp(value, 0, MaxRadius);
                UpdateMaterial();
            }
        }
        #endregion

        #region Unity Lifecycle
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

        private void Update()
        {
            if (_rectTransform == null) return;

            Vector2 currentSize = _rectTransform.rect.size;
            if (currentSize != _lastSize)
            {
                _lastSize = currentSize;
                UpdateAspectRatio();
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(_material);
                else
#endif
                    Destroy(_material);
            }
        }
        #endregion

        #region Public Methods
        public void SetRadius(float radius)
        {
            radius = Mathf.Clamp(radius, 0, MaxRadius);
            _topLeftRadius = radius;
            _topRightRadius = radius;
            _bottomRightRadius = radius;
            _bottomLeftRadius = radius;
            UpdateMaterial();
        }

        public void SetAllRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            _topLeftRadius = Mathf.Clamp(topLeft, 0, MaxRadius);
            _topRightRadius = Mathf.Clamp(topRight, 0, MaxRadius);
            _bottomRightRadius = Mathf.Clamp(bottomRight, 0, MaxRadius);
            _bottomLeftRadius = Mathf.Clamp(bottomLeft, 0, MaxRadius);
            UpdateMaterial();
        }
        #endregion

        #region Private Methods
        private void SetupMaterial()
        {
            if (_image == null) return;

            if (_material == null)
            {
                Shader shader = Shader.Find("UI/RoundedImage");
                if (shader == null)
                {
                    Debug.LogError("[RoundedImageEffect] 找不到 Shader: UI/RoundedImage");
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
            float aspectRatio = size.y / size.x;
            _material.SetFloat(AspectRatioProperty, aspectRatio);
        }
        #endregion
    }
}
