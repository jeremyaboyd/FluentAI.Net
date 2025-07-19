namespace FluentAI.Net.Models
{
    /// <summary>
    /// Reason why the AI response finished
    /// </summary>
    public enum AIFinishReason
    {
        Stop,
        ToolCalls,
        MaxTokens,
        ContentFilter
    }
} 