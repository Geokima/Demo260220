using UnityEngine;
using UnityEngine.UI;
using System;

namespace Game.Main
{
    public class LauncherUI : MonoBehaviour
    {
        [Header("UI 组件")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text statusText;
        [SerializeField] private Text percentText;
        [SerializeField] private Button enterButton;

        [Header("平滑参数")]
        [SerializeField] private float smoothSpeed = 2f;

        private float _targetProgress;
        private string _targetStatus;

        private void Awake()
        {
            if (enterButton != null) enterButton.gameObject.SetActive(false);
            Launcher.OnProgressUpdate += OnProgressUpdate;
        }

        private void OnDestroy() => Launcher.OnProgressUpdate -= OnProgressUpdate;

        private void OnProgressUpdate(float progress, string status)
        {
            _targetProgress = progress;
            _targetStatus = status;
        }

        private void Update()
        {
            if (progressSlider == null) return;

            var oldValue = progressSlider.value;
            progressSlider.value = Mathf.MoveTowards(progressSlider.value, _targetProgress, Time.deltaTime * smoothSpeed);

            if (progressSlider.value > oldValue && statusText != null)
                statusText.text = _targetStatus;

            if (percentText != null)
                percentText.text = $"{(progressSlider.value * 100):f0}%";

            if (progressSlider.value >= 1f && enterButton != null && !enterButton.gameObject.activeSelf)
                enterButton.gameObject.SetActive(true);
        }
    }
}
