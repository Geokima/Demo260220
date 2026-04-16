using UnityEngine;

namespace Game.Gameplay.Demo1.UI
{
    public interface IScenePanelSystem : ISystem
    {
        void SetPanel(SceneMode mode, GameObject panel);
        void SetAllPanels(GameObject selection, GameObject shop, GameObject work, GameObject treasure, GameObject battle, GameObject reward, GameObject gameOver);
    }
}
