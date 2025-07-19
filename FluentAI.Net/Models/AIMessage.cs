namespace FluentAI.Net.Models
{
    /// <summary>
    /// Provider-agnostic message representation
    /// </summary>
    public class AIMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string? ToolCallId { get; set; }
        public List<AIToolCall>? ToolCalls { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
    }
} 