using UnityEngine;

namespace Game.Gameplay.Demo1.UI
{
    public class ScenePanelSystem : AbstractSystem, IScenePanelSystem
    {
        private GameObject _selectionPanel;
        private GameObject _shopPanel;
        private GameObject _workPanel;
        private GameObject _treasurePanel;
        private GameObject _battlePanel;
        private GameObject _rewardPanel;
        private GameObject _gameOverPanel;

        public override void Init()
        {
            var model = this.GetModel<Demo1Model>();
            model.CurrentSceneMode.Register(OnSceneModeChanged);
        }

        public void SetPanel(SceneMode mode, GameObject panel)
        {
            switch (mode)
            {
                case SceneMode.Selection:
                    _selectionPanel = panel;
                    break;
                case SceneMode.Shop:
                    _shopPanel = panel;
                    break;
                case SceneMode.Work:
                    _workPanel = panel;
                    break;
                case SceneMode.Treasure:
                    _treasurePanel = panel;
                    break;
                case SceneMode.Battle:
                case SceneMode.BattleSelect:
                    _battlePanel = panel;
                    break;
                case SceneMode.Reward:
                    _rewardPanel = panel;
                    break;
                case SceneMode.GameOver:
                    _gameOverPanel = panel;
                    break;
            }
        }

        public void SetAllPanels(
            GameObject selection,
            GameObject shop,
            GameObject work,
            GameObject treasure,
            GameObject battle,
            GameObject reward,
            GameObject gameOver)
        {
            _selectionPanel = selection;
            _shopPanel = shop;
            _workPanel = work;
            _treasurePanel = treasure;
            _battlePanel = battle;
            _rewardPanel = reward;
            _gameOverPanel = gameOver;
        }

        private void OnSceneModeChanged(SceneMode mode)
        {
            HideAllPanels();
            ShowPanel(mode);
        }

        private void HideAllPanels()
        {
            if (_selectionPanel != null) _selectionPanel.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_workPanel != null) _workPanel.SetActive(false);
            if (_treasurePanel != null) _treasurePanel.SetActive(false);
            if (_battlePanel != null) _battlePanel.SetActive(false);
            if (_rewardPanel != null) _rewardPanel.SetActive(false);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
        }

        private void ShowPanel(SceneMode mode)
        {
            GameObject panel = mode switch
            {
                SceneMode.Selection => _selectionPanel,
                SceneMode.Shop => _shopPanel,
                SceneMode.Work => _workPanel,
                SceneMode.Treasure => _treasurePanel,
                SceneMode.Battle or SceneMode.BattleSelect => _battlePanel,
                SceneMode.Reward => _rewardPanel,
                SceneMode.GameOver => _gameOverPanel,
                _ => null
            };

            if (panel != null)
                panel.SetActive(true);
        }
    }
}
