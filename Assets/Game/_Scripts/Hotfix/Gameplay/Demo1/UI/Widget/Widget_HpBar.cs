using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_HpBar : MonoBehaviour
    {
        public Image FillImage;
        public Text HPText;
        public Text SheildText;
        public Text PoisonText;
        public string Format = "{0}/{1}";

        public void SetHp(int current, int max)
        {
            if (max > 0)
                FillImage.fillAmount = (float)current / max;
            HPText.text = string.Format(Format, current, max);
        }
        
        public void SetSheild(int current)
        {
            SheildText.text = current.ToString();
        }
        
        public void SetPoison(int current)
        {
            PoisonText.text = current.ToString();
        }
    }
}
