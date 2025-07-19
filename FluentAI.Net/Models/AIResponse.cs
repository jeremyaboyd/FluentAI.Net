namespace FluentAI.Net.Models
{
    /// <summary>
    /// Provider-agnostic response representation
    /// </summary>
    public class AIResponse
    {
        public string? Content { get; set; }
        public List<AIToolCall>? ToolCalls { get; set; }
        public AIFinishReason FinishReason { get; set; }
    }
} 