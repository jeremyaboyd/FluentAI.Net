using System.ComponentModel;

namespace FluentAI.Net.Attributes
{
    /// <summary>
    /// Custom attribute for documenting types, methods, and parameters for better AI understanding
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Property)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; set; }
        public object[]? EnumValues { get; set; }

        public DescriptionAttribute(string description, object[]? enumValues = null)
        {
            Description = description;
            EnumValues = enumValues;
        }
    }
} 