using System.Collections;
using System.Collections.Generic;
using Game.Main;
using UnityEngine;
using UnityEngine.UI;

public class PreloadProgressBar : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private Image _progressBarFill;
    [SerializeField] private Button _BtnStart;

    private void Awake()
    {
        _label.text = "";
        _progressBarFill.fillAmount = 0;
        _BtnStart.onClick.AddListener(() =>
        {
            MainEntry.Instance.LaunchGame();
            Destroy(gameObject);
        });
    }

    void Update()
    {
        if (MainEntry.Instance.Progress >= 0.8f)
            _label.text = "检查版本完成";
        else if (MainEntry.Instance.Progress >= 0.6f)
            _label.text = "下载资源";
        else if (MainEntry.Instance.Progress >= 0.4f)
            _label.text = "更新资源清单";
        else if (MainEntry.Instance.Progress >= 0.2f)
            _label.text = "请求版本号";
        else
            _label.text = "检查资源包";

        _progressBarFill.fillAmount = MainEntry.Instance.Progress;
        if (_progressBarFill.fillAmount == 1f)
        {
            _progressBarFill.gameObject.SetActive(false);
            _label.text = "点击任意地方继续...";
            _BtnStart.gameObject.SetActive(true);
        }
    }
}
