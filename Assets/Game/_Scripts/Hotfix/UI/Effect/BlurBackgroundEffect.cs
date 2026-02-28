using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(RawImage))]
    public class BlurBackgroundEffect : MonoBehaviour
    {
        #region Constants
        private const int BlurIterations = 3;
        #endregion

        #region SerializeField
        [Header("Blur Settings")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _blurRange = 1.0f;

        [Header("Target Setup")]
        [SerializeField] private Camera _sourceCamera;
        [SerializeField] private RenderTextureFormat _rtFormat = RenderTextureFormat.ARGB32;
        #endregion

        #region Private Fields
        private RawImage _rawImage;
        private Material _blurMaterial;
        private RenderTexture[] _downRT;
        private RenderTexture[] _upRT;
        #endregion

        #region Properties
        public float BlurRange
        {
            get => _blurRange;
            set
            {
                _blurRange = value;
                UpdateBlur();
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _blurMaterial = new Material(Shader.Find("Hidden/DualBlur"));
            if (_sourceCamera == null) _sourceCamera = Camera.main;
        }

        private void OnEnable()
        {
            InitializeRT();
            UpdateBlur();
        }

        private void OnDisable()
        {
            ReleaseRT();
        }
        #endregion

        #region Public Methods
        public void UpdateBlur()
        {
            _rawImage.rectTransform.localScale = Vector3.zero;
            ExecuteBlur();
            _rawImage.rectTransform.localScale = Vector3.one;
        }

        public void SetBlurRange(float value)
        {
            _blurRange = value;
            _blurMaterial.SetFloat("_BlurRange", _blurRange);
        }
        #endregion

        #region Private Methods
        private void InitializeRT()
        {
            int width = Screen.width;
            int height = Screen.height;

            _downRT = new RenderTexture[BlurIterations];
            _upRT = new RenderTexture[BlurIterations];

            for (int i = 0; i < BlurIterations; i++)
            {
                width = Mathf.Max(width / 2, 1);
                height = Mathf.Max(height / 2, 1);

                _downRT[i] = CreateRT(width, height);
                _upRT[i] = CreateRT(width, height);
            }
        }

        private RenderTexture CreateRT(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0, _rtFormat)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            return rt;
        }

        private void ReleaseRT()
        {
            if (_downRT == null || _upRT == null) return;

            for (int i = 0; i < _downRT.Length; i++)
            {
                _downRT[i]?.Release();
                _upRT[i]?.Release();
            }
        }

        private void ExecuteBlur()
        {
            var sourceRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, _rtFormat);
            _sourceCamera.targetTexture = sourceRT;
            _sourceCamera.Render();
            _sourceCamera.targetTexture = null;

            _blurMaterial.SetFloat("_BlurRange", _blurRange);

            RenderTexture currentRT = sourceRT;

            for (int i = 0; i < _downRT.Length; i++)
            {
                Graphics.Blit(currentRT, _downRT[i], _blurMaterial, 0);
                currentRT = _downRT[i];
            }

            for (int i = _downRT.Length - 1; i >= 0; i--)
            {
                Graphics.Blit(currentRT, _upRT[i], _blurMaterial, 1);
                currentRT = _upRT[i];
            }

            if (_rawImage.texture == null)
            {
                var color = _rawImage.color;
                float brightness = Mathf.Pow(1 - color.a, 0.4545f);
                color = new Color(brightness, brightness, brightness, 1f);
                _rawImage.color = color;
            }
            _rawImage.texture = currentRT;

            RenderTexture.ReleaseTemporary(sourceRT);
        }
        #endregion
    }
}
