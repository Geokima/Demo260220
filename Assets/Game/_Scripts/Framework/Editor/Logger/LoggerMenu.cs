using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Framework.Editor.Logger
{
    /// <summary>
    /// 日志宏开关菜单工具
    /// </summary>
    public static class LoggerMenu
    {
        private const string NORMAL_MACRO = "DEBUG_LOG_NORMAL";
        private const string WARNING_MACRO = "DEBUG_LOG_WARNING";
        private const string ERROR_MACRO = "DEBUG_LOG_ERROR";

        #region Normal Log

        [MenuItem("Framework/Logger/Normal Log")]
        public static void ToggleNormal() => ToggleMacro(NORMAL_MACRO);

        [MenuItem("Framework/Logger/Normal Log", true)]
        public static bool ToggleNormalValidate()
        {
            Menu.SetChecked("Framework/Logger/Normal Log", HasMacro(NORMAL_MACRO));
            return true;
        }

        #endregion

        #region Warning Log

        [MenuItem("Framework/Logger/Warning Log")]
        public static void ToggleWarning() => ToggleMacro(WARNING_MACRO);

        [MenuItem("Framework/Logger/Warning Log", true)]
        public static bool ToggleWarningValidate()
        {
            Menu.SetChecked("Framework/Logger/Warning Log", HasMacro(WARNING_MACRO));
            return true;
        }

        #endregion

        #region Error Log

        [MenuItem("Framework/Logger/Error Log")]
        public static void ToggleError() => ToggleMacro(ERROR_MACRO);

        [MenuItem("Framework/Logger/Error Log", true)]
        public static bool ToggleErrorValidate()
        {
            Menu.SetChecked("Framework/Logger/Error Log", HasMacro(ERROR_MACRO));
            return true;
        }

        #endregion

        #region Internal Logic

        private static bool HasMacro(string macro)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            return symbols.Split(';').Contains(macro);
        }

        private static void ToggleMacro(string macro)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var symbolList = symbols.Split(';').ToList();

            if (symbolList.Contains(macro))
            {
                symbolList.Remove(macro);
            }
            else
            {
                symbolList.Add(macro);
            }

            var newSymbols = string.Join(";", symbolList.Where(s => !string.IsNullOrEmpty(s)));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newSymbols);
            
            UnityEngine.Debug.Log($"[LoggerMenu] {(symbolList.Contains(macro) ? "Enabled" : "Disabled")} {macro} for {targetGroup}");
        }

        #endregion
    }
}
