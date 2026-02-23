using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Framework;

namespace Game.Tests
{
    /// <summary>
    /// 框架监控面板 - 使用反射获取框架内部状态
    /// </summary>
    public class FrameworkMonitor : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("显示设置")]
        [SerializeField] private bool _showGui = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F12;

        [Header("刷新设置")]
        [SerializeField] private float _containerRefreshInterval = 3f;
        
        #endregion

        #region Private Fields - Window
        
        private Vector2 _scrollPosition;
        private Rect _windowRect;
        private bool _isMinimized = false;
        private const float _minimizedHeight = 30f;
        private bool _isResizing = false;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartSize;
        
        #endregion

        #region Private Fields - Container Page
        
        private bool _showSystems = true;
        private bool _showModels = true;
        private bool _showEvents = true;
        private List<string> _cachedSystems = new List<string>();
        private List<string> _cachedModels = new List<string>();
        private List<EventInfo> _cachedEventInfos = new List<EventInfo>();
        private float _lastRefreshTime;
        
        private class EventInfo
        {
            public string EventType;
            public List<string> HandlerNames = new List<string>();
        }
        
        #endregion

        #region Private Fields - Event History Page
        
        private int _currentPage = 0;
        private readonly string[] _pageNames = { "容器", "事件记录" };
        private List<EventRecord> _eventRecords = new List<EventRecord>();
        private int _maxEventRecords = 100;
        private int _expandedEventIndex = -1;
        private Dictionary<string, List<string>> _eventDataCache = new Dictionary<string, List<string>>();
        
        private class EventRecord
        {
            public DateTime RealTime;
            public string Sender;
            public string EventType;
            public object EventData;
        }
        
        #endregion

        #region Private Fields - GUI Styles
        
        private GUIStyle _flatButtonStyle;
        private GUIStyle _flatBoxStyle;
        private GUIStyle _flatLabelStyle;
        private bool _stylesInitialized;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            _windowRect = new Rect(Screen.width - 410, 10, 400, 400);
            RefreshData();
            EventSystem.OnEventSent += OnEventSent;
        }
        
        private void OnDestroy()
        {
            EventSystem.OnEventSent -= OnEventSent;
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _showGui = !_showGui;

            if (!_isMinimized && Time.time - _lastRefreshTime >= _containerRefreshInterval)
                RefreshData();
        }
        
        private void OnGUI()
        {
            if (!_showGui) return;

            if (_isMinimized)
            {
                DrawMinimized();
            }
            else
            {
                DrawMaximized();
            }
        }
        
        #endregion

        #region GUI Styles
        
        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _flatButtonStyle = new GUIStyle(GUI.skin.button);
            _flatButtonStyle.normal.background = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 1f));
            _flatButtonStyle.hover.background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 1f));
            _flatButtonStyle.active.background = MakeTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f));
            _flatButtonStyle.normal.textColor = Color.white;
            _flatButtonStyle.hover.textColor = Color.white;
            _flatButtonStyle.active.textColor = Color.white;
            _flatButtonStyle.border = new RectOffset(0, 0, 0, 0);
            _flatButtonStyle.margin = new RectOffset(2, 2, 2, 2);
            _flatButtonStyle.padding = new RectOffset(8, 8, 4, 4);

            _flatBoxStyle = new GUIStyle(GUI.skin.box);
            _flatBoxStyle.normal.background = MakeTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            _flatBoxStyle.border = new RectOffset(0, 0, 0, 0);
            _flatBoxStyle.margin = new RectOffset(2, 2, 2, 2);
            _flatBoxStyle.padding = new RectOffset(4, 4, 4, 4);

            _flatLabelStyle = new GUIStyle(GUI.skin.label);
            _flatLabelStyle.normal.textColor = Color.white;

            _stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        #endregion

        #region Window Drawing
        
        private void DrawMinimized()
        {
            var minimizedRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, _minimizedHeight);
            minimizedRect = GUILayout.Window(0, minimizedRect, DrawMinimizedWindow, "FrameworkMonitor", GUILayout.ExpandWidth(true));
            _windowRect.x = minimizedRect.x;
            _windowRect.y = minimizedRect.y;
            _windowRect.width = minimizedRect.width;
        }

        private void DrawMaximized()
        {
            _windowRect = GUILayout.Window(0, _windowRect, DrawWindow, "FrameworkMonitor");
        }

        private void DrawMinimizedWindow(int windowId)
        {
            InitStyles();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Mathf.RoundToInt(1f / Time.deltaTime)} FPS", _flatLabelStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("+", _flatButtonStyle, GUILayout.Width(25)))
                _isMinimized = false;
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private void DrawWindow(int windowId)
        {
            InitStyles();
            
            DrawTitleBar();
            DrawPageTabs();
            DrawPageContent();
            DrawResizeHandle();
            
            GUI.DragWindow();
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Mathf.RoundToInt(1f / Time.deltaTime)} FPS", _flatLabelStyle, GUILayout.Width(60));
            GUILayout.Label($"Time: {Time.time:F2}s", _flatLabelStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-", _flatButtonStyle, GUILayout.Width(25)))
                _isMinimized = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawPageTabs()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _pageNames.Length; i++)
            {
                var isActive = i == _currentPage;
                var btnStyle = isActive ? _flatButtonStyle : _flatBoxStyle;
                if (GUILayout.Button(_pageNames[i], btnStyle, GUILayout.Width(80)))
                    _currentPage = i;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawPageContent()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (_currentPage)
            {
                case 0:
                    DrawContainerPage();
                    break;
                case 1:
                    DrawEventHistoryPage();
                    break;
            }

            GUILayout.EndScrollView();
        }

        private void DrawResizeHandle()
        {
            GUILayoutUtility.GetRect(20, 20, GUILayout.ExpandWidth(true));
            GUI.Box(new Rect(_windowRect.width - 25, _windowRect.height - 25, 20, 20), "::");
            HandleWindowResize();
        }
        
        #endregion

        #region Window Resize
        
        private void HandleWindowResize()
        {
            var e = UnityEngine.Event.current;
            var resizeArea = new Rect(_windowRect.width - 25, _windowRect.height - 25, 25, 25);
            
            if (_isResizing && !Input.GetMouseButton(0))
            {
                _isResizing = false;
                return;
            }
            
            if (_isResizing)
            {
                var screenMouse = Input.mousePosition;
                screenMouse.y = Screen.height - screenMouse.y;
                var deltaX = screenMouse.x - _resizeStartMouse.x;
                var deltaY = screenMouse.y - _resizeStartMouse.y;
                _windowRect.width = Mathf.Max(200, _resizeStartSize.x + deltaX);
                _windowRect.height = Mathf.Max(200, _resizeStartSize.y + deltaY);
                return;
            }
            
            if (e.type == EventType.MouseDown && resizeArea.Contains(e.mousePosition))
            {
                _isResizing = true;
                var mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                _resizeStartMouse = mousePos;
                _resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
                e.Use();
            }
        }
        
        #endregion

        #region Container Page
        
        private void DrawContainerPage()
        {
            DrawSystemsSection();
            GUILayout.Space(10);
            DrawModelsSection();
            GUILayout.Space(10);
            DrawEventsSection();
        }

        private void DrawSystemsSection()
        {
            var icon = _showSystems ? "●" : "○";
            var btnStyle = new GUIStyle(_flatButtonStyle);
            btnStyle.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button($"{icon} Systems ({_cachedSystems.Count})", btnStyle))
                _showSystems = !_showSystems;
            if (!_showSystems) return;

            foreach (var system in _cachedSystems)
            {
                GUILayout.Label($"  {system}", _flatLabelStyle);
            }
        }

        private void DrawModelsSection()
        {
            var icon = _showModels ? "●" : "○";
            var btnStyle = new GUIStyle(_flatButtonStyle);
            btnStyle.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button($"{icon} Models ({_cachedModels.Count})", btnStyle))
                _showModels = !_showModels;
            if (!_showModels) return;

            foreach (var model in _cachedModels)
            {
                GUILayout.Label($"  {model}", _flatLabelStyle);
            }
        }

        private void DrawEventsSection()
        {
            var icon = _showEvents ? "●" : "○";
            var btnStyle = new GUIStyle(_flatButtonStyle);
            btnStyle.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button($"{icon} Events ({_cachedEventInfos.Count})", btnStyle))
                _showEvents = !_showEvents;
            if (!_showEvents) return;

            foreach (var eventInfo in _cachedEventInfos)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label($"- {eventInfo.EventType}", _flatLabelStyle, GUILayout.Width(150));
                
                GUILayout.BeginVertical();
                foreach (var handlerName in eventInfo.HandlerNames)
                {
                    GUILayout.Label($"- {handlerName}", _flatLabelStyle);
                }
                GUILayout.EndVertical();
                
                GUILayout.EndHorizontal();
            }
        }
        
        #endregion

        #region Event History Page
        
        private void DrawEventHistoryPage()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"记录数: {_eventRecords.Count}/{_maxEventRecords}", _flatLabelStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("清空", _flatButtonStyle, GUILayout.Width(50)))
                _eventRecords.Clear();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (_eventRecords.Count == 0)
            {
                GUILayout.Label("暂无事件记录", _flatLabelStyle);
                return;
            }

            for (int i = _eventRecords.Count - 1; i >= 0; i--)
            {
                DrawEventRecord(i);
                GUILayout.Space(2);
            }
        }

        private void DrawEventRecord(int index)
        {
            var record = _eventRecords[index];
            var isExpanded = _expandedEventIndex == index;
            
            var btnStyle = new GUIStyle(_flatButtonStyle);
            btnStyle.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button($"[{record.RealTime:HH:mm:ss}] {record.Sender} -> {record.EventType}", btnStyle))
            {
                _expandedEventIndex = isExpanded ? -1 : index;
            }
            
            if (isExpanded)
            {
                GUILayout.BeginVertical(_flatBoxStyle);
                DrawEventData(record.EventData, record.EventType);
                GUILayout.EndVertical();
            }
        }

        private void DrawEventData(object eventData, string eventType)
        {
            if (eventData == null)
            {
                GUILayout.Label("null", _flatLabelStyle);
                return;
            }

            var fieldNames = GetCachedFieldNames(eventType, eventData.GetType());

            foreach (var name in fieldNames)
            {
                try
                {
                    var value = GetFieldOrPropertyValue(eventData, name);
                    GUILayout.Label($"  {name}: {value}", _flatLabelStyle, GUILayout.ExpandWidth(false));
                }
                catch
                {
                    GUILayout.Label($"  {name}: <error>", _flatLabelStyle, GUILayout.ExpandWidth(false));
                }
            }
        }

        private List<string> GetCachedFieldNames(string cacheKey, Type type)
        {
            if (!_eventDataCache.TryGetValue(cacheKey, out var fieldNames))
            {
                fieldNames = new List<string>();
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    fieldNames.Add(field.Name);
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (prop.CanRead) fieldNames.Add(prop.Name);
                _eventDataCache[cacheKey] = fieldNames;
            }
            return fieldNames;
        }

        private object GetFieldOrPropertyValue(object obj, string name)
        {
            var type = obj.GetType();
            var field = type.GetField(name);
            var prop = type.GetProperty(name);
            return field?.GetValue(obj) ?? prop?.GetValue(obj);
        }
        
        #endregion

        #region Data Refresh
        
        private void RefreshData()
        {
            _cachedSystems = GetSystems();
            _cachedModels = GetModels();
            _cachedEventInfos = GetRegisteredEventInfos();
            _lastRefreshTime = Time.time;
        }

        private void OnEventSent(Type senderType, object eventData)
        {
            _eventRecords.Add(new EventRecord
            {
                RealTime = DateTime.Now,
                Sender = senderType.Name,
                EventType = eventData.GetType().Name,
                EventData = eventData
            });
            
            if (_eventRecords.Count > _maxEventRecords)
                _eventRecords.RemoveAt(0);
        }
        
        #endregion

        #region Reflection Helpers
        
        private List<string> GetSystems()
        {
            return GetInstancesFromContainer<ISystem>();
        }

        private List<string> GetModels()
        {
            return GetInstancesFromContainer<IModel>();
        }

        private List<string> GetInstancesFromContainer<T>() where T : class
        {
            var result = new List<string>();
            var architecture = GameArchitecture.Instance;

            try
            {
                var containerField = GetFieldIncludingBase(architecture.GetType(), "_container");
                if (containerField == null) return result;

                var container = containerField.GetValue(architecture);
                if (container == null) return result;

                var instancesField = GetFieldIncludingBase(container.GetType(), "_instances");
                if (instancesField == null) return result;

                var instances = instancesField.GetValue(container) as Dictionary<Type, object>;
                if (instances == null) return result;

                foreach (var kvp in instances)
                {
                    if (kvp.Value is T)
                        result.Add(kvp.Key.Name);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FrameworkMonitor] GetInstancesFromContainer<{typeof(T).Name}> failed: {ex.Message}");
            }

            return result;
        }

        private List<EventInfo> GetRegisteredEventInfos()
        {
            var result = new List<EventInfo>();
            var architecture = GameArchitecture.Instance;

            try
            {
                var eventSystemField = GetFieldIncludingBase(architecture.GetType(), "_eventSystem");
                if (eventSystemField == null) 
                {
                    Debug.LogWarning("[FrameworkMonitor] _eventSystem field not found");
                    return result;
                }

                var eventSystem = eventSystemField.GetValue(architecture);
                if (eventSystem == null) 
                {
                    Debug.LogWarning("[FrameworkMonitor] _eventSystem is null");
                    return result;
                }

                var typeEventsField = GetFieldIncludingBase(eventSystem.GetType(), "_typeEvents");
                if (typeEventsField == null) 
                {
                    Debug.LogWarning("[FrameworkMonitor] _typeEvents field not found");
                    return result;
                }

                var typeEvents = typeEventsField.GetValue(eventSystem) as System.Collections.IDictionary;
                if (typeEvents == null) 
                {
                    Debug.LogWarning("[FrameworkMonitor] _typeEvents is null or not IDictionary");
                    return result;
                }

                foreach (System.Collections.DictionaryEntry kvp in typeEvents)
                {
                    var keyType = kvp.Key as Type;
                    if (keyType == null) continue;
                    
                    var eventInfo = new EventInfo { EventType = keyType.Name };
                    var eventValue = kvp.Value;
                    if (eventValue == null) continue;
                    
                    var onEventField = GetFieldIncludingBase(eventValue.GetType(), "_onEvent");
                    if (onEventField != null)
                    {
                        var onEvent = onEventField.GetValue(eventValue) as Delegate;
                        if (onEvent != null)
                        {
                            foreach (var handler in onEvent.GetInvocationList())
                                eventInfo.HandlerNames.Add(handler.Method.Name);
                        }
                    }
                    result.Add(eventInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FrameworkMonitor] GetRegisteredEventInfos failed: {ex.Message}");
            }

            return result;
        }

        private FieldInfo GetFieldIncludingBase(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }
        
        #endregion
    }
}
