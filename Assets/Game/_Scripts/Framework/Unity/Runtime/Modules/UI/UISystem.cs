using System;
using System.Collections.Generic;
using Framework.Modules.Res;
using UnityEngine;
using UnityEngine.UI;
using static Framework.Logger;

namespace Framework.Modules.UI
{
    /// <summary>
    /// UI 系统实现类
    /// </summary>
    public class UISystem : AbstractSystem, IUISystem
    {
        #region Fields

        private readonly Dictionary<string, UIPanel> _loadedPanels = new();
        private readonly HashSet<string> _activePanels = new();
        private readonly Dictionary<UILayer, RectTransform> _layerRoots = new();
        private readonly Dictionary<UILayer, int> _layerCounters = new();
        private readonly List<UIPanel> _navigationStack = new();
        private static Canvas _canvasRoot;
        private static RectTransform _canvasRootRect;

        #endregion

        #region Properties

        public static Canvas CanvasRoot => _canvasRoot;

        public static RectTransform CanvasRootRect => _canvasRootRect;

        /// <inheritdoc />
        public int NavigationStackCount => _navigationStack.Count;

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
            CreateEventSystem();
            CreateCanvasRoot();
            CreateLayerRoots();
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                _layerCounters[layer] = 0;
        }

        /// <inheritdoc />
        public override void Deinit()
        {
            CloseAll(Enum.GetValues(typeof(UILayer)) as UILayer[]);
            _loadedPanels.Clear();
            _activePanels.Clear();
            _navigationStack.Clear();
            _layerCounters.Clear();
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public T GetPanel<T>() where T : UIPanel
        {
            _loadedPanels.TryGetValue(typeof(T).Name, out var panel);
            return panel as T;
        }

        /// <inheritdoc />
        public bool IsOpen<T>() where T : UIPanel
        {
            return _activePanels.Contains(typeof(T).Name);
        }

        /// <inheritdoc />
        public void Open<T>(object data = null) where T : UIPanel
        {
            Log($"[UI] Open panel: {typeof(T).Name}");
            var name = typeof(T).Name;

            if (_loadedPanels.TryGetValue(name, out var panel))
            {
                Reopen(panel, data);
                return;
            }

            panel = LoadPanel<T>();
            if (panel == null) return;

            _loadedPanels[panel.GetType().Name] = panel;
            _activePanels.Add(panel.GetType().Name);

            if (panel.Layer == UILayer.Navigation)
            {
                if (_navigationStack.Count > 0)
                    _navigationStack[^1].OnPause();
                _navigationStack.Add(panel);
            }

            if (!panel.FixedOrder)
            {
                _layerCounters[panel.Layer]++;
                panel.Canvas.sortingOrder = (int)panel.Layer + _layerCounters[panel.Layer] * 10;
            }

            panel.OnOpen(data);
        }

        /// <inheritdoc />
        public void Close<T>() where T : UIPanel
        {
            Log($"[UI] Close panel: {typeof(T).Name}");
            var name = typeof(T).Name;
            if (!_loadedPanels.TryGetValue(name, out var panel)) return;

            _activePanels.Remove(name);

            if (panel.Layer == UILayer.Navigation)
            {
                bool wasTop = _navigationStack.Count > 0 && _navigationStack[^1] == panel;
                _navigationStack.Remove(panel);

                panel.OnClose();
                RefreshNavigationSorting();

                if (wasTop && _navigationStack.Count > 0)
                    _navigationStack[^1].OnResume();
            }
            else
            {
                panel.OnClose();
            }
        }

        /// <inheritdoc />
        public void CloseAll(params UILayer[] layers)
        {
            if (layers.Length == 0) return;

            var layerSet = new HashSet<UILayer>(layers);
            var panelsToClose = new List<UIPanel>();

            foreach (var pair in _loadedPanels)
            {
                if (layerSet.Contains(pair.Value.Layer))
                    panelsToClose.Add(pair.Value);
            }

            bool hasNavigation = false;
            foreach (var panel in panelsToClose)
            {
                if (panel.Layer == UILayer.Navigation)
                {
                    _navigationStack.Remove(panel);
                    hasNavigation = true;
                }

                _activePanels.Remove(panel.GetType().Name);
                panel.OnClose();
            }

            if (hasNavigation)
            {
                RefreshNavigationSorting();
                if (_navigationStack.Count > 0)
                    _navigationStack[^1].OnResume();
            }
        }

        #endregion

        #region Private Methods

        private void Reopen(UIPanel panel, object data)
        {
            if (panel.Layer == UILayer.Navigation)
            {
                int index = _navigationStack.IndexOf(panel);

                if (index >= 0)
                {
                    if (panel.IsSingleton)
                    {
                        for (int i = _navigationStack.Count - 1; i > index; i--)
                        {
                            var toClose = _navigationStack[i];
                            _navigationStack.RemoveAt(i);
                            _activePanels.Remove(toClose.GetType().Name);
                            toClose.OnClose();
                        }
                        LogWarning($"[UI] Singleton panel {panel.GetType().Name} reopened, closed {_navigationStack.Count - index - 1} panels above");
                    }
                    else
                    {
                        _navigationStack.RemoveAt(index);
                    }
                }

                if (_navigationStack.Count > 0)
                    _navigationStack[^1].OnPause();

                _navigationStack.Add(panel);

                if (!panel.FixedOrder)
                {
                    _layerCounters[panel.Layer]++;
                    panel.Canvas.sortingOrder = (int)panel.Layer + _layerCounters[panel.Layer] * 10;
                }

                _activePanels.Add(panel.GetType().Name);
                panel.OnOpen(data);
            }
            else
            {
                if (!panel.FixedOrder)
                {
                    _layerCounters[panel.Layer]++;
                    panel.Canvas.sortingOrder = (int)panel.Layer + _layerCounters[panel.Layer] * 10;
                }
                _activePanels.Add(panel.GetType().Name);
                panel.OnOpen(data);
            }
        }

        private T LoadPanel<T>() where T : UIPanel
        {
            var name = typeof(T).Name;
            var resSystem = this.GetSystem<IResSystem>();
            var prefab = resSystem.Load<GameObject>(name);

            if (prefab == null)
            {
                LogError($"[UI] Failed to load panel prefab: {name}");
                return null;
            }
            var panel = prefab.GetComponent<T>();
            if (panel == null)
            {
                LogError($"[UI] Panel component not found on prefab: {name}");
                return null;
            }

            var obj = UnityEngine.Object.Instantiate(prefab, _layerRoots[panel.Layer]);
            
            panel = obj.GetComponent<T>();
            panel.Architecture = Architecture;
            panel.OnInit();
            return panel;
        }

        private void RefreshNavigationSorting()
        {
            _layerCounters[UILayer.Navigation] = 0;
            foreach (var panel in _navigationStack)
            {
                if (!panel.FixedOrder)
                {
                    _layerCounters[UILayer.Navigation]++;
                    panel.Canvas.sortingOrder = (int)UILayer.Navigation + _layerCounters[UILayer.Navigation] * 10;
                }
            }
        }

        private void CreateEventSystem()
        {
            var existingEventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem != null) return;

            var obj = new GameObject("EventSystem");
            obj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            obj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private void CreateCanvasRoot()
        {
            if (_canvasRoot != null) return;

            var obj = new GameObject("RootCanvas");
            _canvasRoot = obj.AddComponent<Canvas>();
            _canvasRootRect = obj.GetComponent<RectTransform>();

            _canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            obj.AddComponent<GraphicRaycaster>();
        }

        private void CreateLayerRoots()
        {
            if (_layerRoots.Count > 0) return;

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var obj = new GameObject($"Layer_{layer}");
                obj.transform.SetParent(_canvasRootRect, false);

                var rect = obj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                var canvas = obj.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = (int)layer;

                obj.AddComponent<GraphicRaycaster>();

                _layerRoots[layer] = rect;
            }
        }

        #endregion
    }
}
