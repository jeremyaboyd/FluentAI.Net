namespace FluentAI.Net.Models
{
    /// <summary>
    /// Provider-agnostic tool call representation
    /// </summary>
    public class AIToolCall
    {
        public string Id { get; set; }
        public string FunctionName { get; set; }
        public string FunctionArguments { get; set; }
    }
} 