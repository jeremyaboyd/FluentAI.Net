using System.Text;
using System.Text.Json;
using FluentAI.Net.Models;

namespace FluentAI.Net.Providers
{
    /// <summary>
    /// Anthropic API provider implementation
    /// </summary>
    public class AnthropicProvider : IAIProvider
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private readonly string model;

        public string ProviderName => "Anthropic";

        public AnthropicProvider(string apiKey, string model)
        {
            this.apiKey = apiKey;
            this.model = model;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            this.httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<AIResponse> CompleteChatAsync(List<AIMessage> messages, AICompletionOptions options)
        {
            var anthropicRequest = ConvertToAnthropicRequest(messages, options);
            var json = JsonSerializer.Serialize(anthropicRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Anthropic API error: {responseJson}");
            }

            var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseJson);
            return ConvertFromAnthropicResponse(anthropicResponse);
        }

        private object ConvertToAnthropicRequest(List<AIMessage> messages, AICompletionOptions options)
        {
            var systemMessage = messages.FirstOrDefault(m => m.Role == "system")?.Content ?? "";
            var conversationMessages = messages.Where(m => m.Role != "system").ToList();

            var anthropicMessages = new List<object>();
            
            foreach (var message in conversationMessages)
            {
                switch (message.Role)
                {
                    case "user":
                        if (message.ImageData != null)
                        {
                            anthropicMessages.Add(new
                            {
                                role = "user",
                                content = new object[]
                                {
                                    new { type = "text", text = message.Content },
                                    new 
                                    { 
                                        type = "image", 
                                        source = new 
                                        { 
                                            type = "base64", 
                                            media_type = message.ImageMimeType ?? "image/jpeg",
                                            data = Convert.ToBase64String(message.ImageData) 
                                        } 
                                    }
                                }
                            });
                        }
                        else
                        {
                            anthropicMessages.Add(new
                            {
                                role = "user",
                                content = message.Content
                            });
                        }
                        break;
                    case "assistant":
                        if (message.ToolCalls != null && message.ToolCalls.Count > 0)
                        {
                            var content = new List<object>();
                            if (!string.IsNullOrEmpty(message.Content))
                            {
                                content.Add(new { type = "text", text = message.Content });
                            }
                            
                            foreach (var toolCall in message.ToolCalls)
                            {
                                content.Add(new
                                {
                                    type = "tool_use",
                                    id = toolCall.Id,
                                    name = toolCall.FunctionName,
                                    input = JsonSerializer.Deserialize<object>(toolCall.FunctionArguments)
                                });
                            }
                            
                            anthropicMessages.Add(new
                            {
                                role = "assistant",
                                content = content
                            });
                        }
                        else
                        {
                            anthropicMessages.Add(new
                            {
                                role = "assistant",
                                content = message.Content
                            });
                        }
                        break;
                    case "tool":
                        anthropicMessages.Add(new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new 
                                { 
                                    type = "tool_result", 
                                    tool_use_id = message.ToolCallId, 
                                    content = message.Content 
                                }
                            }
                        });
                        break;
                }
            }

            var request = new
            {
                model = model,
                max_tokens = 4096,
                system = systemMessage,
                messages = anthropicMessages
            };

            // Add tools if available
            if (options.Tools.Count > 0)
            {
                var tools = options.Tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    input_schema = JsonSerializer.Deserialize<object>(t.ParametersSchema)
                }).ToArray();

                return new
                {
                    model = model,
                    max_tokens = 4096,
                    system = systemMessage,
                    messages = anthropicMessages,
                    tools = tools
                };
            }

            return request;
        }

        private AIResponse ConvertFromAnthropicResponse(AnthropicResponse response)
        {
            var result = new AIResponse
            {
                Content = "",
                FinishReason = response.stop_reason switch
                {
                    "end_turn" => AIFinishReason.Stop,
                    "tool_use" => AIFinishReason.ToolCalls,
                    "max_tokens" => AIFinishReason.MaxTokens,
                    _ => AIFinishReason.Stop
                }
            };

            var toolCalls = new List<AIToolCall>();
            var textContent = new StringBuilder();

            foreach (var content in response.content)
            {
                if (content.type == "text")
                {
                    textContent.Append(content.text);
                }
                else if (content.type == "tool_use")
                {
                    toolCalls.Add(new AIToolCall
                    {
                        Id = content.id,
                        FunctionName = content.name,
                        FunctionArguments = JsonSerializer.Serialize(content.input)
                    });
                }
            }

            result.Content = textContent.ToString();
            if (toolCalls.Count > 0)
            {
                result.ToolCalls = toolCalls;
                result.FinishReason = AIFinishReason.ToolCalls;
            }

            return result;
        }

        private class AnthropicResponse
        {
            public string stop_reason { get; set; }
            public AnthropicContent[] content { get; set; }
        }

        private class AnthropicContent
        {
            public string type { get; set; }
            public string text { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public object input { get; set; }
        }
    }
} 