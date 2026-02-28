using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Framework.Editor
{
    [Overlay(typeof(SceneView), "UI Analyzer", true)]
    public class UIAnalyzerOverlay : Overlay
    {
        #region Fields
        private bool _showRaycastTargets = true;
        private bool _showDrawCallInfo = false;
        private bool _showOverdraw = false;

        private Color _normalColor = new Color(0, 1, 1, 0.15f);
        private Color _warningColor = new Color(1, 0.92f, 0.016f, 0.15f);
        private Color _errorColor = new Color(1, 0, 0, 0.15f);
        private Color _overdrawColor = new Color(1, 1, 1, 0.1f);

        private Dictionary<Canvas, DrawCallInfo> _canvasInfo = new Dictionary<Canvas, DrawCallInfo>();
        #endregion

        #region Overlay
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement { style = { paddingTop = 4, paddingBottom = 4 } };

            var raycastToggle = new UnityEngine.UIElements.Toggle("Raycast Targets") { value = _showRaycastTargets };
            raycastToggle.RegisterValueChangedCallback(evt =>
            {
                _showRaycastTargets = evt.newValue;
                SceneView.RepaintAll();
            });
            root.Add(raycastToggle);

            var drawcallToggle = new UnityEngine.UIElements.Toggle("DrawCall Info") { value = _showDrawCallInfo };
            drawcallToggle.RegisterValueChangedCallback(evt =>
            {
                _showDrawCallInfo = evt.newValue;
                SceneView.RepaintAll();
            });
            root.Add(drawcallToggle);

            var overdrawToggle = new UnityEngine.UIElements.Toggle("Overdraw") { value = _showOverdraw };
            overdrawToggle.RegisterValueChangedCallback(evt =>
            {
                _showOverdraw = evt.newValue;
                SceneView.RepaintAll();
            });
            root.Add(overdrawToggle);

            SceneView.duringSceneGui += OnSceneGUI;
            return root;
        }

        public override void OnWillBeDestroyed()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            base.OnWillBeDestroyed();
        }
        #endregion

        #region Scene GUI
        private void OnSceneGUI(SceneView sceneView)
        {
            if (_showDrawCallInfo || _showOverdraw)
                AnalyzeDrawCalls();

            if (_showRaycastTargets)
                DrawRaycastTargets();

            if (_showDrawCallInfo)
                DrawDrawCallInfo(sceneView);

            if (_showOverdraw)
                DrawOverdraw();
        }

        private void DrawRaycastTargets()
        {
            var graphics = Object.FindObjectsOfType<Graphic>();
            foreach (var graphic in graphics)
            {
                if (graphic == null || !graphic.gameObject.activeInHierarchy) continue;

                var rectTransform = graphic.rectTransform;
                if (rectTransform == null) continue;

                bool raycastEnabled = graphic.raycastTarget;
                bool hasInteraction = HasInteractionComponent(graphic);

                Color color;
                if (raycastEnabled && hasInteraction)
                    color = _normalColor;
                else if (raycastEnabled && !hasInteraction)
                    color = _warningColor;
                else if (!raycastEnabled && hasInteraction)
                    color = _errorColor;
                else
                    continue;

                DrawRect(rectTransform, color);
            }
        }

        private void DrawDrawCallInfo(SceneView sceneView)
        {
            Handles.BeginGUI();

            int yOffset = 10;
            foreach (var kvp in _canvasInfo)
            {
                var canvas = kvp.Key;
                var info = kvp.Value;

                string text = $"{canvas.name}: {info.EstimatedDrawCalls} DC / {info.TotalGraphics} UI";
                if (info.EstimatedDrawCalls > 10)
                    text += " [!]";

                GUI.Label(new Rect(10, yOffset, 250, 20), text, new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = info.EstimatedDrawCalls > 10 ? Color.yellow : Color.white },
                    fontSize = 12
                });
                yOffset += 22;
            }

            Handles.EndGUI();
        }

        private void DrawOverdraw()
        {
            foreach (var kvp in _canvasInfo)
            {
                var info = kvp.Value;

                foreach (var batch in info.Batches)
                {
                    foreach (var graphic in batch.Value)
                    {
                        if (graphic.color.a <= 0) continue;

                        var rect = GetWorldRect(graphic.rectTransform);
                        float alpha = Mathf.Min(0.15f, 0.02f * info.OverdrawElements);

                        Handles.DrawSolidRectangleWithOutline(
                            new Vector3[]
                            {
                                new Vector3(rect.x, rect.y, 0),
                                new Vector3(rect.x + rect.width, rect.y, 0),
                                new Vector3(rect.x + rect.width, rect.y + rect.height, 0),
                                new Vector3(rect.x, rect.y + rect.height, 0)
                            },
                            new Color(1, 1, 1, alpha),
                            Color.clear
                        );
                    }
                }
            }
        }

        private void DrawRect(RectTransform rectTransform, Color color)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Handles.color = color;
            Handles.DrawAAConvexPolygon(corners[0], corners[1], corners[2], corners[3]);

            Handles.color = new Color(color.r, color.g, color.b, 0.8f);
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);
        }
        #endregion

        #region Analysis
        private void AnalyzeDrawCalls()
        {
            _canvasInfo.Clear();

            var canvases = Object.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (!canvas.isActiveAndEnabled) continue;

                var info = new DrawCallInfo();
                var graphics = canvas.GetComponentsInChildren<Graphic>(true);

                foreach (var graphic in graphics)
                {
                    if (!graphic.isActiveAndEnabled) continue;
                    if (graphic.canvasRenderer.cull) continue;

                    info.TotalGraphics++;

                    var material = graphic.materialForRendering;
                    var texture = graphic.mainTexture;

                    var batchKey = new BatchKey
                    {
                        Material = material,
                        Texture = texture,
                        Depth = GetDepth(graphic.transform)
                    };

                    if (!info.Batches.ContainsKey(batchKey))
                    {
                        info.Batches[batchKey] = new List<Graphic>();
                        info.EstimatedDrawCalls++;
                    }
                    info.Batches[batchKey].Add(graphic);

                    if (graphic.color.a > 0 && graphic.raycastTarget)
                        info.OverdrawElements++;
                }

                _canvasInfo[canvas] = info;
            }
        }

        private bool HasInteractionComponent(Graphic graphic)
        {
            var go = graphic.gameObject;
            return go.GetComponent<UnityEngine.UI.Button>() != null ||
                   go.GetComponent<InputField>() != null ||
                   go.GetComponent<UnityEngine.UI.Toggle>() != null ||
                   go.GetComponent<UnityEngine.UI.Slider>() != null ||
                   go.GetComponent<Scrollbar>() != null ||
                   go.GetComponent<Dropdown>() != null ||
                   go.GetComponent<ScrollRect>() != null ||
                   go.GetComponent<UnityEngine.EventSystems.EventTrigger>() != null;
        }

        private Rect GetWorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            float minX = corners[0].x;
            float minY = corners[0].y;
            float maxX = corners[0].x;
            float maxY = corners[0].y;

            for (int i = 1; i < 4; i++)
            {
                minX = Mathf.Min(minX, corners[i].x);
                minY = Mathf.Min(minY, corners[i].y);
                maxX = Mathf.Max(maxX, corners[i].x);
                maxY = Mathf.Max(maxY, corners[i].y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private int GetDepth(Transform transform)
        {
            int depth = 0;
            while (transform.parent != null)
            {
                depth++;
                transform = transform.parent;
            }
            return depth;
        }
        #endregion

        #region Data Classes
        private class DrawCallInfo
        {
            public int TotalGraphics;
            public int EstimatedDrawCalls;
            public int OverdrawElements;
            public Dictionary<BatchKey, List<Graphic>> Batches = new Dictionary<BatchKey, List<Graphic>>();
        }

        private struct BatchKey
        {
            public Material Material;
            public Texture Texture;
            public int Depth;
        }
        #endregion
    }
}
