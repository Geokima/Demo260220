using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_CardValueLabel : MonoBehaviour
    {
        public Text Text;

        public void SetText(string text)
        {
            if (int.TryParse(text, out int value))
            {
                Text.text = value.ToString();
                gameObject.SetActive(value != 0);
            }
            else if (float.TryParse(text, out float floatValue))
            {
                Text.text = floatValue.ToString("F1");
                gameObject.SetActive(floatValue != 0);
            }
            else
            {
                Text.text = text;
                gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }
    }
}
