using System;

namespace Framework.Utils
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIfAttribute : Attribute
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
