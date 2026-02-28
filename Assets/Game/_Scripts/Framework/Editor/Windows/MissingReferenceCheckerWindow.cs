#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Framework.Editor
{
    public class MissingReferenceCheckerWindow : EditorWindow
    {
        #region Fields
        private Vector2 _scrollPosition;
        private List<MissingInfo> _results = new List<MissingInfo>();
        private int _checkedCount;
        private int _missingCount;
        #endregion

        #region Menu
        [MenuItem("Tools/Missing Reference Checker")]
        public static void ShowWindow() => GetWindow<MissingReferenceCheckerWindow>("Missing Reference Checker");
        #endregion

        #region GUI
        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("检查当前场景", GUILayout.Height(30))) ScanScene();
            if (GUILayout.Button("检查选中物体", GUILayout.Height(30))) ScanSelection();
            if (GUILayout.Button("检查整个项目", GUILayout.Height(30))) ScanProject();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("提示: 检测基于场景文件(.unity)内容。若场景未保存，缺失的脚本和图片引用名称可能不准确", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"已检查: {_checkedCount} | 缺失: {_missingCount}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_results.Count > 0)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                foreach (var info in _results) DrawItem(info);
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("清空", GUILayout.Height(25)))
                {
                    _results.Clear();
                    _missingCount = 0;
                    _checkedCount = 0;
                }
            }
            else if (_checkedCount > 0)
            {
                EditorGUILayout.HelpBox("未发现缺失引用", MessageType.Info);
            }
        }

        private void DrawItem(MissingInfo info)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            var icon = info.Type == MissingType.Script
                ? EditorGUIUtility.IconContent("console.warnicon")
                : EditorGUIUtility.IconContent("console.erroricon");
            GUILayout.Label(icon, GUILayout.Width(20));

            EditorGUILayout.BeginVertical();

            if (info.Target && GUILayout.Button(info.Target.name, EditorStyles.linkLabel))
            {
                Selection.activeGameObject = info.Target;
                EditorGUIUtility.PingObject(info.Target);
            }
            else if (!string.IsNullOrEmpty(info.AssetPath) && GUILayout.Button(info.AssetPath, EditorStyles.linkLabel))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(info.AssetPath);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            EditorGUILayout.LabelField(info.Detail, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Scan
        private void ScanScene()
        {
            Reset();
            foreach (var go in FindObjectsOfType<GameObject>()) ScanGameObject(go);
            LogResult("场景");
        }

        private void ScanProject()
        {
            Reset();
            EditorUtility.DisplayProgressBar("扫描中", "", 0);
            try
            {
                var paths = AssetDatabase.GetAllAssetPaths();
                for (int i = 0; i < paths.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("扫描中", paths[i], (float)i / paths.Length);
                    if (paths[i].StartsWith("Assets/") && paths[i].EndsWith(".prefab"))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                        if (prefab) ScanPrefab(prefab, paths[i]);
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            LogResult("项目");
        }

        private void ScanSelection()
        {
            Reset();
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("请先选中物体");
                return;
            }
            foreach (var go in Selection.gameObjects) ScanGameObject(go);
            LogResult("选中物体");
        }

        private void ScanGameObject(GameObject go)
        {
            _checkedCount++;
            CheckComponents(go);
            foreach (Transform child in go.transform) ScanGameObject(child.gameObject);
        }

        private void ScanPrefab(GameObject prefab, string path)
        {
            _checkedCount++;
            CheckComponents(prefab, path);
            foreach (Transform child in prefab.transform) ScanPrefabChild(child.gameObject, path);
        }

        private void ScanPrefabChild(GameObject go, string path)
        {
            _checkedCount++;
            CheckComponents(go, path);
            foreach (Transform child in go.transform) ScanPrefabChild(child.gameObject, path);
        }

        private void Reset()
        {
            _results.Clear();
            _missingCount = 0;
            _checkedCount = 0;
        }

        private void LogResult(string type) =>
            Debug.Log($"[缺失引用] {type}检查完成: {_checkedCount}个对象, {_missingCount}个缺失");
        #endregion

        #region Check
        private void CheckComponents(GameObject go, string assetPath = null)
        {
            var components = go.GetComponents<Component>();
            var scriptGuids = GetAllScriptGuids(go);

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    string scriptName = GetScriptNameFromGuids(scriptGuids, i);
                    AddResult(MissingType.Script, go, assetPath, scriptName);
                }
                else
                    CheckReferences(components[i], go, assetPath);
            }
        }

        private List<(string guid, string compId)> GetAllScriptGuids(GameObject go)
        {
            var result = new List<(string, string)>();
            var file = GetFilePath(go);
            if (string.IsNullOrEmpty(file)) return result;

            var lines = File.ReadAllLines(file);
            var goId = FindFileId(lines, go.name);
            if (goId == null) return result;

            var compIds = FindAllComponentIds(lines, goId);
            foreach (var compId in compIds)
            {
                var (guid, hasScript) = FindScriptGuid(lines, compId);
                result.Add((guid, compId));
            }
            return result;
        }

        private string GetScriptNameFromGuids(List<(string guid, string compId)> guids, int index)
        {
            if (index < 0 || index >= guids.Count)
                return $"Missing Script (Index:{index})";

            var (guid, compId) = guids[index];
            if (string.IsNullOrEmpty(guid))
                return $"Missing Script (CompID:{compId})";

            var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
            var scriptName = string.IsNullOrEmpty(scriptPath) ? "Missing Script" : Path.GetFileNameWithoutExtension(scriptPath);
            return $"{scriptName} (GUID:{guid})";
        }

        private void CheckReferences(Object comp, GameObject go, string assetPath)
        {
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                    prop.objectReferenceValue == null &&
                    prop.objectReferenceInstanceIDValue != 0)
                {
                    string guid = GetReferenceGuid(comp, prop.propertyPath, go, assetPath);
                    string detail = $"{comp.GetType().Name}.{prop.displayName}";

                    if (!string.IsNullOrEmpty(guid))
                    {
                        var refPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(refPath))
                            detail += $" → {Path.GetFileNameWithoutExtension(refPath)} (GUID:{guid})";
                        else
                            detail += $" (GUID:{guid})";
                    }

                    AddResult(MissingType.Reference, go, assetPath, detail);
                }
            }
        }

        private void AddResult(MissingType type, GameObject go, string path, string detail)
        {
            _results.Add(new MissingInfo { Type = type, Target = go, AssetPath = path, Detail = detail });
            _missingCount++;
        }
        #endregion

        #region GUID Lookup
        private string GetScriptName(GameObject go, int index)
        {
            var file = GetFilePath(go);
            if (string.IsNullOrEmpty(file))
                return $"Missing Script (FileNotFound:{go.name})";

            var lines = File.ReadAllLines(file);
            var goId = FindFileId(lines, go.name);
            if (goId == null)
                return $"Missing Script (GoNotFound:{go.name})";

            var compId = FindComponentId(lines, goId, index);
            if (compId == null)
                return $"Missing Script (CompNotFound:GoID={goId},Index={index})";

            var (guid, isMissing) = FindScriptGuid(lines, compId);
            if (string.IsNullOrEmpty(guid))
                return $"Missing Script (CompID:{compId})";

            var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
            var scriptName = string.IsNullOrEmpty(scriptPath) ? "Missing Script" : Path.GetFileNameWithoutExtension(scriptPath);
            return $"{scriptName} (GUID:{guid})";
        }

        private string GetScriptNameByIndex(GameObject go, int missingIndex)
        {
            var file = GetFilePath(go);
            if (string.IsNullOrEmpty(file))
                return $"Missing Script (FileNotFound:{go.name})";

            var lines = File.ReadAllLines(file);
            var goId = FindFileId(lines, go.name);
            if (goId == null)
                return $"Missing Script (GoNotFound:{go.name})";

            var compIds = FindAllComponentIds(lines, goId);
            int missingCount = 0;
            foreach (var compId in compIds)
            {
                var (guid, hasScriptField) = FindScriptGuid(lines, compId);
                if (hasScriptField)
                {
                    if (missingCount == missingIndex)
                    {
                        if (string.IsNullOrEmpty(guid))
                            return $"Missing Script (CompID:{compId})";
                        var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                        var scriptName = string.IsNullOrEmpty(scriptPath) ? "Missing Script" : Path.GetFileNameWithoutExtension(scriptPath);
                        return $"{scriptName} (GUID:{guid})";
                    }
                    missingCount++;
                }
            }
            return $"Missing Script (Index:{missingIndex})";
        }

        private List<string> FindAllComponentIds(string[] lines, string goId)
        {
            var result = new List<string>();
            bool found = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains($"&{goId}")) found = true;
                if (found && lines[i].Contains("m_Component:"))
                {
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        if (!lines[j].TrimStart().StartsWith("-")) break;
                        var m = System.Text.RegularExpressions.Regex.Match(lines[j], @"fileID:\s*(\d+)");
                        if (m.Success) result.Add(m.Groups[1].Value);
                    }
                    break;
                }
            }
            return result;
        }

        private string GetReferenceGuid(Object comp, string propPath, GameObject go, string assetPath)
        {
            var file = GetFilePath(go) ?? assetPath;
            if (string.IsNullOrEmpty(file)) return null;

            var lines = File.ReadAllLines(file);
            var compId = FindComponentFileId(lines, comp);
            if (compId == null) return null;

            return FindPropertyGuid(lines, compId, propPath);
        }

        private string GetFilePath(GameObject go)
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(go);
            if (stage != null) return stage.assetPath;
            if (go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.path) && File.Exists(go.scene.path))
                return go.scene.path;
            if (PrefabUtility.IsPartOfAnyPrefab(go))
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (prefab) return AssetDatabase.GetAssetPath(prefab);
            }
            return AssetDatabase.GetAssetPath(go);
        }
        #endregion

        #region YAML Parse
        private string FindFileId(string[] lines, string name)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("--- !u!1 &") || lines[i].StartsWith("--- !u!4 &"))
                {
                    var id = lines[i].Substring(lines[i].IndexOf('&') + 1);
                    for (int j = i + 1; j < lines.Length && !lines[j].StartsWith("--- !u!"); j++)
                    {
                        if (lines[j].StartsWith("  m_Name:"))
                        {
                            var lineName = lines[j].Substring(lines[j].IndexOf(':') + 1).Trim();
                            if (lineName == name)
                                return id;
                            break;
                        }
                    }
                }
            }
            return null;
        }

        private string FindComponentId(string[] lines, string goId, int index)
        {
            bool found = false;
            int count = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains($"&{goId}")) found = true;
                if (found && lines[i].Contains("m_Component:"))
                {
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        if (!lines[j].TrimStart().StartsWith("-")) break;
                        if (count == index)
                        {
                            var m = Regex.Match(lines[j], @"fileID:\s*(\d+)");
                            return m.Success ? m.Groups[1].Value : null;
                        }
                        count++;
                    }
                    break;
                }
            }
            return null;
        }

        private (string guid, bool hasScriptField) FindScriptGuid(string[] lines, string compId)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains($"&{compId}"))
                {
                    bool hasScriptField = false;
                    string guid = null;
                    for (int j = i + 1; j < lines.Length && !lines[j].StartsWith("--- !u!"); j++)
                    {
                        if (lines[j].Contains("m_Script:"))
                        {
                            hasScriptField = true;
                            var line = lines[j];
                            if (line.Contains("fileID: 0") || line.Contains("guid: 00000000000000000000000000000000"))
                                return (null, true);
                            var m = Regex.Match(line, @"guid:\s*([a-f0-9]+)");
                            if (m.Success)
                                guid = m.Groups[1].Value;
                            break;
                        }
                    }
                    return (guid, hasScriptField);
                }
            }
            return (null, false);
        }

        private string FindComponentFileId(string[] lines, Object comp)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("--- !u!"))
                {
                    var id = lines[i].Substring(lines[i].IndexOf('&') + 1);
                    for (int j = i + 1; j < lines.Length && !lines[j].StartsWith("--- !u!"); j++)
                    {
                        if (lines[j].Contains("m_EditorClassIdentifier"))
                            return id;
                    }
                }
            }
            return null;
        }

        private string FindPropertyGuid(string[] lines, string compId, string propPath)
        {
            bool found = false;
            var parts = propPath.Split('.');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains($"&{compId}")) found = true;
                if (!found) continue;

                foreach (var part in parts)
                {
                    if (lines[i].Contains($"{part}:"))
                    {
                        var m = Regex.Match(lines[i], @"guid:\s*([a-f0-9]+)");
                        if (m.Success) return m.Groups[1].Value;
                    }
                }
            }
            return null;
        }
        #endregion

        #region Types
        private enum MissingType { Script, Reference }

        private class MissingInfo
        {
            public MissingType Type;
            public GameObject Target;
            public string AssetPath;
            public string Detail;
        }
        #endregion
    }
}
#endif
