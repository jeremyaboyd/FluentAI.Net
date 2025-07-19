using FluentAI.Net.Models;

namespace FluentAI.Net.Providers
{
    /// <summary>
    /// Interface for AI provider implementations
    /// </summary>
    public interface IAIProvider
    {
        Task<AIResponse> CompleteChatAsync(List<AIMessage> messages, AICompletionOptions options);
        string ProviderName { get; }
    }
} 