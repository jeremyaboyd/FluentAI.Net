namespace FluentAI.Net.Models
{
    /// <summary>
    /// Provider-agnostic tool definition
    /// </summary>
    public class AITool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ParametersSchema { get; set; }
    }
} 