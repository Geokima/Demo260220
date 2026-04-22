using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_HpBar : MonoBehaviour
    {
        public Image FillImage;
        public Text Text;
        public string Format = "{0}/{1}";

        public void SetHp(int current, int max)
        {
            if (max > 0)
                FillImage.fillAmount = (float)current / max;
            Text.text = string.Format(Format, current, max);
        }
    }
}
