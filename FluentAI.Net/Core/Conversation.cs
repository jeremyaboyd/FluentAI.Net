using System.Text.Json;
using FluentAI.Net.Models;
using OpenAI.Chat;

namespace FluentAI.Net.Core
{
    /// <summary>
    /// Represents a conversation session with state management and message history
    /// </summary>
    public class Conversation
    {
        FluentClient Client { get; }

        public Dictionary<string, object> State { get; } = new Dictionary<string, object>();
        private List<AIMessage> Messages { get; } = new List<AIMessage>();

        private string systemPrompt;

        internal Conversation(FluentClient client, string prompt)
        {
            Client = client;
            systemPrompt = prompt;
        }

        public Tout? Send<Tout>(object input, params Delegate[] functions)
        {
            return SendAsync<Tout>(input, functions).GetAwaiter().GetResult();
        }

        public async Task<Tout?> SendAsync<Tout>(object input, params Delegate[] functions)
        {
            string inputJson = JsonSerializer.Serialize(input);
            string stateJson = JsonSerializer.Serialize(State);

            string systemMessage = $"""
                # System Message:
                
                {systemPrompt}
                ---
                # State:
                {stateJson}
                """;

            Messages.Add(new AIMessage { Role = "user", Content = inputJson });

            return await Client.SendMessageAsync<Tout>(systemMessage, Messages, functions);
        }

        public TOut? SendWithImage<TOut>(object input, byte[] raw, params Delegate[] functions)
        {
            return SendWithImageAsync<TOut>(input, raw, functions).GetAwaiter().GetResult();
        }

        public async Task<TOut?> SendWithImageAsync<TOut>(object input, byte[] raw, params Delegate[] functions)
        {
            string inputJson = JsonSerializer.Serialize(input);
            string stateJson = JsonSerializer.Serialize(State);

            string systemMessage = $"""
                # System Message:
                
                {systemPrompt}
                ---
                # State:
                {stateJson}
                """;

            var userMessage = new AIMessage { 
                Role = "user", 
                Content = inputJson, 
                ImageData = raw, 
                ImageMimeType = "image/jpeg" 
            };

            Messages.Add(userMessage);

            return await Client.SendMessageAsync<TOut>(systemMessage, Messages, functions);
        }

#pragma warning disable OPENAI001
        public string GetResponse(string message, OpenAI.Responses.ResponseReasoningOptions? options = null)
        {
            return GetResponseAsync(message, options).GetAwaiter().GetResult();
        }

        public async Task<string> GetResponseAsync(string message, OpenAI.Responses.ResponseReasoningOptions? options = null)
        {
            Messages.Add(new AIMessage { Role = "user", Content = message });

            return await Client.GetResponseAsync(systemPrompt, Messages, options);
        }
#pragma warning restore OPENAI001
    }
} 