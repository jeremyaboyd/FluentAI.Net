using OpenAI;
using OpenAI.Chat;
using FluentAI.Net.Models;

namespace FluentAI.Net.Providers
{
    /// <summary>
    /// OpenAI API provider implementation
    /// </summary>
    public class OpenAIProvider : IAIProvider
    {
        private readonly OpenAIClient client;
        private readonly string model;

        public string ProviderName => "OpenAI";

        public OpenAIProvider(string apiKey, string model)
        {
            this.client = new OpenAIClient(apiKey);
            this.model = model;
        }

        public async Task<AIResponse> CompleteChatAsync(List<AIMessage> messages, AICompletionOptions options)
        {
            var chatClient = client.GetChatClient(model);
            var chatMessages = ConvertToOpenAIMessages(messages);
            var chatOptions = ConvertToOpenAIOptions(options);

            var response = await chatClient.CompleteChatAsync(chatMessages, chatOptions);

            return ConvertFromOpenAIResponse(response);
        }

        private List<ChatMessage> ConvertToOpenAIMessages(List<AIMessage> messages)
        {
            var result = new List<ChatMessage>();
            
            foreach (var message in messages)
            {
                switch (message.Role)
                {
                    case "system":
                        result.Add(new SystemChatMessage(message.Content));
                        break;
                    case "user":
                        if (message.ImageData != null)
                        {
                            var imagePart = ChatMessageContentPart.CreateImagePart(
                                BinaryData.FromBytes(message.ImageData),
                                message.ImageMimeType ?? "image/jpeg",
                                ChatImageDetailLevel.Auto
                            );
                            result.Add(new UserChatMessage(
                                ChatMessageContentPart.CreateTextPart(message.Content),
                                imagePart
                            ));
                        }
                        else
                        {
                            result.Add(new UserChatMessage(message.Content));
                        }
                        break;
                    case "assistant":
                        if (message.ToolCalls != null && message.ToolCalls.Count > 0)
                        {
                            var toolCalls = message.ToolCalls.Select(tc => 
                                ChatToolCall.CreateFunctionToolCall(tc.Id, tc.FunctionName, BinaryData.FromString(tc.FunctionArguments))
                            ).ToList();
                            result.Add(new AssistantChatMessage(toolCalls));
                        }
                        else
                        {
                            result.Add(new AssistantChatMessage(message.Content));
                        }
                        break;
                    case "tool":
                        result.Add(new ToolChatMessage(message.ToolCallId, message.Content));
                        break;
                }
            }
            
            return result;
        }

        private ChatCompletionOptions ConvertToOpenAIOptions(AICompletionOptions options)
        {
            var chatOptions = new ChatCompletionOptions();
            
            if (options.ResponseFormat == "json_schema")
            {
                chatOptions.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: options.ResponseFormatName,
                    jsonSchemaFormatDescription: options.ResponseFormatDescription,
                    jsonSchema: BinaryData.FromString(options.ResponseFormatSchema));
            }

            foreach (var tool in options.Tools)
            {
                var chatTool = ChatTool.CreateFunctionTool(
                    functionName: tool.Name,
                    functionDescription: tool.Description,
                    functionParameters: BinaryData.FromString(tool.ParametersSchema),
                    functionSchemaIsStrict: true);
                chatOptions.Tools.Add(chatTool);
            }

            return chatOptions;
        }

        private AIResponse ConvertFromOpenAIResponse(ChatCompletion response)
        {
            var result = new AIResponse
            {
                Content = response.Content.FirstOrDefault()?.Text,
                FinishReason = response.FinishReason switch
                {
                    ChatFinishReason.Stop => AIFinishReason.Stop,
                    ChatFinishReason.ToolCalls => AIFinishReason.ToolCalls,
                    ChatFinishReason.Length => AIFinishReason.MaxTokens,
                    ChatFinishReason.ContentFilter => AIFinishReason.ContentFilter,
                    _ => AIFinishReason.Stop
                }
            };

            if (response.ToolCalls?.Count > 0)
            {
                result.ToolCalls = response.ToolCalls.Select(tc => new AIToolCall
                {
                    Id = tc.Id,
                    FunctionName = tc.FunctionName,
                    FunctionArguments = tc.FunctionArguments.ToString()
                }).ToList();
            }

            return result;
        }
    }
} 