#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Framework.Editor
{
    public class UIBindingGeneratorWindow : EditorWindow
    {
        #region Static Check TMP
        private static bool _hasTMP;

        [InitializeOnLoadMethod]
        private static void CheckTMP()
        {
            _hasTMP = System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "Unity.TextMeshPro");
        }
        #endregion

        #region Fields
        private string _className = "";
        private string _generatedCode = "";
        private Vector2 _scrollPos;
        private Vector2 _listScrollPos;
        private GameObject _selectedObject;
        private List<UIComponentInfo> _components = new List<UIComponentInfo>();
        private string _lastValidFolderPath = "Assets";

        private class UIComponentInfo
        {
            public bool Selected = true;
            public string TypeName;
            public string FieldName;
            public string Path;
            public Transform Transform;
            public int Depth;
        }
        #endregion

        #region Lifecycle
        [MenuItem("Tools/UI/Generate Binding", false, 10)]
        private static void OpenWindow()
        {
            var window = GetWindow<UIBindingGeneratorWindow>("UI Binding Generator");
            window.minSize = new Vector2(600, 500);
            window.GenerateFromSelection();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            Repaint();
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            var currentSelection = Selection.activeGameObject;
            if (currentSelection != null && currentSelection != _selectedObject)
            {
                GenerateFromSelection();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("类名", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _className = EditorGUILayout.TextField(_className);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshCode();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.LabelField("检测到的组件", EditorStyles.boldLabel);

            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos, GUILayout.ExpandHeight(true));
            foreach (var comp in _components)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(comp.Depth * 15);

                bool isValid = IsValidIdentifier(comp.FieldName);
                EditorGUI.BeginDisabledGroup(!isValid);
                bool newSelected = EditorGUILayout.Toggle(comp.Selected, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();

                if (isValid && newSelected != comp.Selected)
                {
                    comp.Selected = newSelected;
                    RefreshCode();
                }
                EditorGUILayout.LabelField(comp.TypeName, GUILayout.Width(100));

                GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel);
                if (!isValid)
                    nameStyle.normal.textColor = Color.yellow;
                EditorGUILayout.LabelField(comp.FieldName, nameStyle);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                foreach (var c in _components)
                {
                    if (IsValidIdentifier(c.FieldName))
                        c.Selected = true;
                }
                RefreshCode();
            }
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                foreach (var c in _components) c.Selected = false;
                RefreshCode();
            }
            if (GUILayout.Button("反选", GUILayout.Width(60)))
            {
                foreach (var c in _components)
                {
                    if (IsValidIdentifier(c.FieldName))
                        c.Selected = !c.Selected;
                }
                RefreshCode();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("生成的代码", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_generatedCode, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);
            string currentPath = GetSelectedFolderPath();
            if (!string.IsNullOrEmpty(currentPath))
                _lastValidFolderPath = currentPath;
            string targetPath = currentPath ?? _lastValidFolderPath;
            string fileName = $"{_className}.Binding.cs";
            string displayPath = targetPath.StartsWith("Packages/") ? "[请选中项目文件夹]" : $"{targetPath}/{fileName}";
            EditorGUILayout.LabelField($"生成路径: {displayPath}", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("复制到剪贴板", GUILayout.Height(30)))
            {
                EditorGUIUtility.systemCopyBuffer = _generatedCode;
                ShowNotification(new GUIContent("已复制!"));
            }

            if (GUILayout.Button("生成文件", GUILayout.Height(30)))
            {
                GenerateFile();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Component Collection
        private void GenerateFromSelection()
        {
            _selectedObject = Selection.activeGameObject;
            if (_selectedObject == null)
                return;

            _className = RemovePrefix(_selectedObject.name, "UI_", "UI");
            _className = _className.TrimStart('_');
            if (!_className.StartsWith("UI_"))
                _className = "UI_" + _className;
            CollectComponents();
            RefreshCode();
        }

        private void CollectComponents()
        {
            _components.Clear();
            if (_selectedObject == null) return;

            CollectComponentsRecursive(_selectedObject.transform, "", 0);
        }

        private void CollectComponentsRecursive(Transform transform, string parentPath, int depth)
        {
            string path = string.IsNullOrEmpty(parentPath) ? transform.name : $"{parentPath}/{transform.name}";
            string name = transform.name;

            var btn = transform.GetComponent<Button>();
            if (btn != null)
                AddComponent("Button", MakeFieldName("Btn", name), path, transform, depth);

            var img = transform.GetComponent<Image>();
            if (img != null)
                AddComponent("Image", MakeFieldName("Img", name), path, transform, depth);

            var rawImg = transform.GetComponent<RawImage>();
            if (rawImg != null)
                AddComponent("RawImage", MakeFieldName("Raw", name), path, transform, depth);

            if (_hasTMP && TryGetTMPText(transform, out string tmpTextType))
            {
                AddComponent(tmpTextType, MakeFieldName("Txt", name), path, transform, depth);
            }
            else
            {
                var txt = transform.GetComponent<Text>();
                if (txt != null)
                    AddComponent("Text", MakeFieldName("Txt", name), path, transform, depth);
            }

            if (_hasTMP && TryGetTMPInputField(transform, out string tmpInputType))
            {
                AddComponent(tmpInputType, MakeFieldName("Input", name), path, transform, depth);
            }
            else
            {
                var input = transform.GetComponent<InputField>();
                if (input != null)
                    AddComponent("InputField", MakeFieldName("Input", name), path, transform, depth);
            }

            if (_hasTMP && TryGetTMPDropdown(transform, out string tmpDropType))
            {
                AddComponent(tmpDropType, MakeFieldName("Drop", name), path, transform, depth);
            }
            else
            {
                var dropdown = transform.GetComponent<Dropdown>();
                if (dropdown != null)
                    AddComponent("Dropdown", MakeFieldName("Drop", name), path, transform, depth);
            }

            var slider = transform.GetComponent<Slider>();
            if (slider != null)
                AddComponent("Slider", MakeFieldName("Slider", name), path, transform, depth);

            var toggle = transform.GetComponent<Toggle>();
            if (toggle != null)
                AddComponent("Toggle", MakeFieldName("Toggle", name), path, transform, depth);

            var scroll = transform.GetComponent<ScrollRect>();
            if (scroll != null)
                AddComponent("ScrollRect", MakeFieldName("Scroll", name), path, transform, depth);

            var scrollbar = transform.GetComponent<Scrollbar>();
            if (scrollbar != null)
                AddComponent("Scrollbar", MakeFieldName("ScrollBar", name), path, transform, depth);

            foreach (Transform child in transform)
            {
                if (child == transform) continue;
                CollectComponentsRecursive(child, path, depth + 1);
            }
        }

        private void AddComponent(string typeName, string fieldName, string path, Transform transform, int depth)
        {
            bool isValid = IsValidIdentifier(fieldName);
            _components.Add(new UIComponentInfo
            {
                TypeName = typeName,
                FieldName = fieldName,
                Path = path,
                Transform = transform,
                Depth = depth,
                Selected = isValid
            });
        }
        #endregion

        #region Code Generation
        private void RefreshCode()
        {
            if (_selectedObject == null) return;
            _generatedCode = GenerateCode();
        }

        private string GenerateCode()
        {
            var selectedComps = _components.Where(c => c.Selected && IsValidIdentifier(c.FieldName)).ToList();
            if (selectedComps.Count == 0) return "// 未选择任何组件";

            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            if (selectedComps.Any(c => c.TypeName.StartsWith("TMP_") || c.TypeName == "TextMeshProUGUI"))
                sb.AppendLine("using TMPro;");
            sb.AppendLine("using Framework.Modules.UI;");
            sb.AppendLine();
            sb.AppendLine($"public partial class {_className} : UIPanel");
            sb.AppendLine("{");

            foreach (var comp in selectedComps)
                sb.AppendLine($"    public {comp.TypeName} {comp.FieldName};");

            sb.AppendLine();
            sb.AppendLine("    partial void InitComponents();");
            sb.AppendLine();
            sb.AppendLine("    protected override void Awake()");
            sb.AppendLine("    {");
            sb.AppendLine("        base.Awake();");

            foreach (var comp in selectedComps)
            {
                if (string.IsNullOrEmpty(comp.Path) || comp.Path == _selectedObject.name)
                    sb.AppendLine($"        {comp.FieldName} = GetComponent<{comp.TypeName}>();");
                else
                {
                    string relativePath = comp.Path.Substring(_selectedObject.name.Length + 1);
                    sb.AppendLine($"        {comp.FieldName} = transform.Find(\"{relativePath}\").GetComponent<{comp.TypeName}>();");
                }
            }

            sb.AppendLine("        InitComponents();");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateFile()
        {
            string path = GetSelectedFolderPath() ?? _lastValidFolderPath;

            if (path.StartsWith("Packages/"))
            {
                ShowNotification(new GUIContent("不能生成到 Packages 文件夹!"));
                return;
            }

            string fileName = $"{_className}.Binding.cs";
            string fullPath = System.IO.Path.Combine(path, fileName);

            System.IO.File.WriteAllText(fullPath, _generatedCode);
            AssetDatabase.Refresh();

            ShowNotification(new GUIContent($"已生成: {fileName}"));
        }

        private string GetSelectedFolderPath()
        {
            foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(assetPath))
                    return assetPath;
                else if (!string.IsNullOrEmpty(assetPath))
                    return System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            }
            return null;
        }
        #endregion

        #region Helpers
        private static string MakeFieldName(string prefix, string objectName)
        {
            string name = objectName.Replace("_", "");

            string[] prefixes = { "Button", "Btn", "Text", "Txt", "Image", "Img", "RawImage", "Raw",
                                  "InputField", "Input", "Dropdown", "Drop", "Slider", "Toggle",
                                  "ScrollRect", "Scroll", "Scrollbar", "ScrollBar" };

            foreach (var p in prefixes)
            {
                if (name.StartsWith(p, System.StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(p.Length);
                    break;
                }
            }

            if (string.IsNullOrEmpty(name))
                name = objectName.Replace("_", "");

            return prefix + name;
        }

        private static string RemovePrefix(string name, params string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return name.Substring(prefix.Length);
            }
            return name;
        }

        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            char first = name[0];
            if (!IsValidIdentifierStart(first))
                return false;

            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!IsValidIdentifierPart(c))
                    return false;
            }

            return true;
        }

        private static bool IsValidIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_' || IsChinese(c);
        }

        private static bool IsValidIdentifierPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || IsChinese(c);
        }

        private static bool IsChinese(char c)
        {
            return c >= 0x4E00 && c <= 0x9FA5;
        }
        #endregion

        #region TMP Detection
        private static bool TryGetTMPText(Transform t, out string typeName)
        {
            typeName = null;
            var comp = t.GetComponent("TMPro.TextMeshProUGUI");
            if (comp != null)
            {
                typeName = "TextMeshProUGUI";
                return true;
            }
            comp = t.GetComponent("TMPro.TMP_Text");
            if (comp != null)
            {
                typeName = "TMP_Text";
                return true;
            }
            return false;
        }

        private static bool TryGetTMPInputField(Transform t, out string typeName)
        {
            typeName = null;
            var comp = t.GetComponent("TMPro.TMP_InputField");
            if (comp != null)
            {
                typeName = "TMP_InputField";
                return true;
            }
            return false;
        }

        private static bool TryGetTMPDropdown(Transform t, out string typeName)
        {
            typeName = null;
            var comp = t.GetComponent("TMPro.TMP_Dropdown");
            if (comp != null)
            {
                typeName = "TMP_Dropdown";
                return true;
            }
            return false;
        }
        #endregion
    }
}
#endif
