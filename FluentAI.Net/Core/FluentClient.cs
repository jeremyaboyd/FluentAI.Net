using System.Text.Json;
using FluentAI.Net.Models;
using FluentAI.Net.Providers;
using FluentAI.Net.Utils;

namespace FluentAI.Net.Core
{
    /// <summary>
    /// Main client class that manages AI provider abstraction and conversation creation
    /// </summary>
    public class FluentClient
    {
        public static TextWriter Logger = null;

        private IAIProvider provider;

        public FluentClient(IAIProvider provider)
        {
            this.provider = provider;
        }

        // Convenience constructors for specific providers
        public static FluentClient UseOpenAI(string apiKey, string model)
        {
            return new FluentClient(new OpenAIProvider(apiKey, model));
        }

        public static FluentClient UseAnthropic(string apiKey, string model)
        {
            return new FluentClient(new AnthropicProvider(apiKey, model));
        }

        public Conversation StartConversation(string systemPrompt)
        {
            return new Conversation(this, systemPrompt);
        }

        public void Log(string message)
        {
            if (Logger != null)
            {
                Logger.WriteLine($"{DateTime.Now:U}: {message}");
            }
        }

        internal Tout? SendMessage<Tout>(string systemMessage, List<AIMessage> messages, Delegate[] functions) where Tout : class
        {
            return SendMessageAsync<Tout>(systemMessage, messages, functions).GetAwaiter().GetResult();
        }
        
        internal async Task<Tout?> SendMessageAsync<Tout>(string systemMessage, List<AIMessage> messages, Delegate[] functions)
        {
            try
            {
                bool isJsonResponse = true;
                var message = messages[^1];
                var outputSchema = SchemaGenerator.SerializeStructuredOutput<Tout>();
                var options = new AICompletionOptions();
                
                if (typeof(Tout) != typeof(string))
                {
                    options.ResponseFormat = "json_schema";
                    options.ResponseFormatName = outputSchema.Name;
                    options.ResponseFormatDescription = outputSchema.Description;
                    options.ResponseFormatSchema = outputSchema.Schema;
                }

                var convertTypes = new Type[] { typeof(string), typeof(int), typeof(float), typeof(double), typeof(bool) };
                if (convertTypes.Contains(typeof(Tout)))
                    isJsonResponse = false;

                foreach (var function in functions)
                {
                    var tool = DelegateToAITool(function);
                    options.Tools.Add(tool);
                }

                // Add system message to the beginning
                var sysMessage = new AIMessage { Role = "system", Content = systemMessage };

                bool keepGoing = false;
                do
                {
                    var now = DateTime.Now;
                    Log($"{now:u}: Sending message to {provider.ProviderName} API");
                    Log($"\t{message.Content}");
                    
                    AIResponse response = await provider.CompleteChatAsync([sysMessage, ..messages], options);
                    
                    now = DateTime.Now;
                    Log($"{now:u}: Received response from {provider.ProviderName} API");

                    switch (response.FinishReason)
                    {
                        case AIFinishReason.Stop:
                            Log($"\t{response.Content}");
                            messages.Add(new AIMessage { Role = "assistant", Content = response.Content });
                            if (isJsonResponse)
                                return JsonSerializer.Deserialize<Tout>(response.Content);
                            else
                                return (Tout)Convert.ChangeType(response.Content, typeof(Tout));

                        case AIFinishReason.ToolCalls:
                            Log($"\tCalling Functions:\n\t\t{string.Join("\n\t\t", response.ToolCalls.Select(tc => $"{tc.FunctionName}({tc.FunctionArguments})"))}");
                            messages.Add(new AIMessage { Role = "assistant", Content = response.Content, ToolCalls = response.ToolCalls });
                            
                            foreach (AIToolCall toolCall in response.ToolCalls)
                            {
                                var function = functions.FirstOrDefault(f => f.Method.Name == toolCall.FunctionName);
                                if (function == null) throw new NotImplementedException();

                                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.FunctionArguments);
                                Dictionary<string, object> convertedParameters = new Dictionary<string, object>();
                                foreach (var kvp in parameters)
                                {
                                    if (kvp.Value is JsonElement element)
                                    {
                                        convertedParameters[kvp.Key] = element.ValueKind switch
                                        {
                                            JsonValueKind.String => element.GetString(),
                                            JsonValueKind.Number => element.TryGetInt32(out int i) ? i : element.GetDouble(),
                                            JsonValueKind.True => true,
                                            JsonValueKind.False => false,
                                            JsonValueKind.Null => null,
                                            JsonValueKind.Object => JsonSerializer.Deserialize<object>(element.GetRawText()),
                                            JsonValueKind.Array => JsonSerializer.Deserialize<object[]>(element.GetRawText()),
                                            _ => element.GetRawText() // For objects or arrays, keep raw JSON
                                        };
                                    }
                                    else
                                    {
                                        convertedParameters[kvp.Key] = kvp.Value; // If already a primitive, keep it
                                    }
                                }

                                var result = function.DynamicInvoke(convertedParameters.Values.ToArray());
                                string resultContent = function.Method.ReturnType != typeof(void) ? 
                                    JsonSerializer.Serialize(result) : "";
                                
                                messages.Add(new AIMessage { 
                                    Role = "tool", 
                                    Content = resultContent,
                                    ToolCallId = toolCall.Id 
                                });
                            }
                            keepGoing = true;
                            break;
                        default:
                            throw new Exception($"Unexpected finish reason: {response.FinishReason}");
                    }
                } while (keepGoing);
            }
            catch (Exception e)
            {
                var now = DateTime.Now;
                Log($"{now:u}: <<Error>> {e.Message}");
                Log($"\t{e.ToString().Replace("\n", "\n\t")}");
            }

            return default;
        }

        private AITool DelegateToAITool(Delegate @delegate)
        {
            var jsonSchema = SchemaGenerator.SerializeMethod(@delegate.Method);
            return new AITool
            {
                Name = jsonSchema.Name,
                Description = jsonSchema.Description,
                ParametersSchema = jsonSchema.Schema
            };
        }
    }
} 