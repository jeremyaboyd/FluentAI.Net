namespace FluentAI.Net.Models
{
    /// <summary>
    /// Options for AI completion requests
    /// </summary>
    public class AICompletionOptions
    {
        public string? ResponseFormat { get; set; }
        public string? ResponseFormatName { get; set; }
        public string? ResponseFormatDescription { get; set; }
        public string? ResponseFormatSchema { get; set; }
        public List<AITool> Tools { get; set; } = new List<AITool>();
    }
} 