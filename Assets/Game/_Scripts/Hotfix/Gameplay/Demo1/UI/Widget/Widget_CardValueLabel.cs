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
                if (value != 0)
                    gameObject.SetActive(true);
                else
                    gameObject.SetActive(false);
            }
        }
    }
}
