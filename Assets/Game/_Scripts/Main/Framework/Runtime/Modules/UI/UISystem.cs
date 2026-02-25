using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Framework.Modules.UI
{
    public class UISystem : AbstractSystem
    {
        private Dictionary<string, UIPanel> _openedPanels = new();
        private Dictionary<UILayer, RectTransform> _layerRoots = new();
        private Dictionary<UILayer, int> _layerCounters = new();
        private List<UIPanel> _navigationStack = new();
        private Canvas _canvasRoot;
        private RectTransform _canvasRootRect;

        public Canvas CanvasRoot => _canvasRoot;
        public RectTransform CanvasRootRect => _canvasRootRect;
        public int NavigationStackCount => _navigationStack.Count;

        public override void Init()
        {
            CreateEventSystem();
            CreateCanvasRoot();
            CreateLayerRoots();
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                _layerCounters[layer] = 0;
        }

        public override void Deinit()
        {
            CloseAll(Enum.GetValues(typeof(UILayer)) as UILayer[]);
            _openedPanels.Clear();
            _navigationStack.Clear();
            _layerCounters.Clear();
        }

        public T GetPanel<T>() where T : UIPanel
        {
            _openedPanels.TryGetValue(typeof(T).Name, out var panel);
            return panel as T;
        }

        public bool IsOpen<T>() where T : UIPanel
        {
            return _openedPanels.ContainsKey(typeof(T).Name);
        }

        public void Open<T>(object data = null) where T : UIPanel
        {
            var name = typeof(T).Name;

            if (_openedPanels.TryGetValue(name, out var panel))
            {
                Reopen(panel, data);
                return;
            }

            panel = LoadPanel<T>();
            if (panel == null) return;

            _openedPanels[panel.GetType().Name] = panel;

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

        public void Close<T>() where T : UIPanel
        {
            var name = typeof(T).Name;
            if (!_openedPanels.TryGetValue(name, out var panel)) return;

            _openedPanels.Remove(name);

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

        public void CloseAll(params UILayer[] layers)
        {
            if (layers.Length == 0) return;

            var layerSet = new HashSet<UILayer>(layers);
            var panelsToClose = new List<UIPanel>();

            foreach (var pair in _openedPanels)
            {
                if (layerSet.Contains(pair.Value.Layer))
                    panelsToClose.Add(pair.Value);
            }

            bool hasNavigation = false;
            foreach (var panel in panelsToClose)
            {
                _openedPanels.Remove(panel.GetType().Name);

                if (panel.Layer == UILayer.Navigation)
                {
                    _navigationStack.Remove(panel);
                    hasNavigation = true;
                }

                panel.OnClose();
            }

            if (hasNavigation)
            {
                RefreshNavigationSorting();
                if (_navigationStack.Count > 0)
                    _navigationStack[^1].OnResume();
            }
        }

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
                            _openedPanels.Remove(toClose.GetType().Name);
                            _navigationStack.RemoveAt(i);
                            toClose.OnClose();
                        }
                        Debug.LogWarning($"[UI] Singleton panel {panel.GetType().Name} reopened, closed {_navigationStack.Count - index - 1} panels above");
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

                panel.OnOpen(data);
            }
            else
            {
                if (!panel.FixedOrder)
                {
                    _layerCounters[panel.Layer]++;
                    panel.Canvas.sortingOrder = (int)panel.Layer + _layerCounters[panel.Layer] * 10;
                }
                panel.OnOpen(data);
            }
        }

        private T LoadPanel<T>() where T : UIPanel
        {
            var name = typeof(T).Name;
            var resSystem = this.GetSystem<Res.ResSystem>();
            var prefab = resSystem.AssetLoader.Load<GameObject>(name);

            if (prefab == null)
            {
                Debug.LogError($"[UI] Failed to load panel prefab: {name}");
                return null;
            }

            var obj = UnityEngine.Object.Instantiate(prefab);
            var panel = obj.GetComponent<T>();

            if (panel == null)
            {
                Debug.LogError($"[UI] Panel component not found on prefab: {name}");
                UnityEngine.Object.Destroy(obj);
                return null;
            }

            obj.transform.SetParent(_layerRoots[panel.Layer], false);
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
    }
}
