using UnityEngine;

namespace Framework.Utils
{
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionField { get; }
        public bool ExpectedValue { get; }

        public ShowIfAttribute(string conditionField, bool expectedValue = true)
        {
            ConditionField = conditionField;
            ExpectedValue = expectedValue;
        }
    }
}
