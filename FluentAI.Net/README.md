# FluentAI.Net

A fluent .NET library for interacting with AI providers (OpenAI, Anthropic Claude) with structured output support.

## Features

- **Multi-Provider Support**: Works with OpenAI and Anthropic Claude APIs
- **Fluent API Design**: Clean, intuitive method chaining
- **Structured Output**: Automatic JSON schema generation and validation
- **Function Calling**: Easy integration of tools and functions
- **Image Support**: Send images alongside text messages
- **Conversation State**: Maintain context across multiple interactions

## Quick Start

### Basic Usage

```csharp
using FluentAI.Net;

// Create a client
var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");

// Start a conversation
var chat = client.StartConversation("You are a helpful assistant.");

// Send a message
var response = chat.Send<string>("Hello, world!");
Console.WriteLine(response);
```

### Anthropic Claude

```csharp
var client = FluentClient.UseAnthropic("your-api-key", "claude-3-sonnet-20240229");
var chat = client.StartConversation("You are a helpful assistant.");

var response = chat.Send<string>("Hello, world!");
Console.WriteLine(response);
```

### Async Support

```csharp
var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");
var chat = client.StartConversation("You are a helpful assistant.");

var response = await chat.SendAsync<string>("Hello, world!");
Console.WriteLine(response);
```

### Multiple Conversations

```csharp
var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");

var chat1 = client.StartConversation("You are a weather expert.");
var chat2 = client.StartConversation("You are a cooking expert.");

var response1 = chat1.Send<string>("What's the weather in Tokyo?", GetWeather);
var response2 = chat2.Send<string>("How do I make pasta?");

Console.WriteLine($"Weather: {response1}");
Console.WriteLine($"Cooking: {response2}");
```

### Structured Output

```csharp
public class WeatherResponse
{
    public string City { get; set; }
    public int Temperature { get; set; }
    public string Condition { get; set; }
}

var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");
var chat = client.StartConversation("You are a weather service API.");

var weather = chat.Send<WeatherResponse>("What's the weather in Paris?", GetWeather);
Console.WriteLine($"Temperature in {weather.City}: {weather.Temperature}°C");
```

### Function Calling

```csharp
[Description("Get current weather for a location")]
static string GetWeather(
    [Description("The city name")] string city,
    [Description("Temperature unit")] string unit = "celsius")
{
    // Your weather API logic here
    return $"The weather in {city} is 22°C and sunny";
}

var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");
var chat = client.StartConversation("You are a helpful assistant with access to weather data.");

var response = chat.Send<string>("What's the weather in London?", GetWeather);
Console.WriteLine(response);
```

### Image Analysis

```csharp
var client = FluentClient.UseOpenAI("your-api-key", "gpt-4-vision-preview");
var chat = client.StartConversation("You are an image analysis expert.");

byte[] imageData = File.ReadAllBytes("path/to/image.jpg");

var analysis = chat.SendWithImage<List<string>>(
    "What objects do you see in this image?", 
    imageData
);

foreach (var item in analysis)
{
    Console.WriteLine($"- {item}");
}
```

### Reasoning with OpenAI (o3-mini, o1, etc.)

```csharp
using OpenAI.Responses;

var client = FluentClient.UseOpenAI("your-api-key", "o3-mini");
var chat = client.StartConversation("You are a strategic thinking assistant.");

// Simple reasoning request
var response = await chat.GetResponseAsync(
    "What's the optimal strategy to win at poker?"
);
Console.WriteLine(response);

// With custom reasoning effort level
var detailedResponse = await chat.GetResponseAsync(
    "Explain the implications of quantum computing on cryptography.",
    new ResponseReasoningOptions()
    {
        ReasoningEffortLevel = ResponseReasoningEffortLevel.High
    }
);
Console.WriteLine(detailedResponse);
```

### Conversation State

```csharp
var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");
var chat = client.StartConversation("You are a shopping assistant.");

// Set some state
chat.State["budget"] = 1000;
chat.State["preferences"] = new[] { "gaming", "portable" };

var response = chat.Send<string>("I want to buy a laptop for $50", AddToCart);
Console.WriteLine(response);
```

## API Reference

### FluentClient

```csharp
public class FluentClient
{
    // Factory methods for creating clients
    public static FluentClient UseOpenAI(string apiKey, string model);
    public static FluentClient UseAnthropic(string apiKey, string model);
    
    // Create conversations
    public Conversation StartConversation(string systemPrompt);
}
```

### Conversation

```csharp
public class Conversation
{
    // State management
    public Dictionary<string, object> State { get; }
    
    // Send messages
    public T? Send<T>(object input, params Delegate[] functions);
    public async Task<T?> SendAsync<T>(object input, params Delegate[] functions);
    
    // Send messages with images
    public T? SendWithImage<T>(object input, byte[] imageData, params Delegate[] functions);
    public async Task<T?> SendWithImageAsync<T>(object input, byte[] imageData, params Delegate[] functions);
    
    // Get text responses with reasoning (OpenAI only)
    public string GetResponse(string message, OpenAI.Responses.ResponseReasoningOptions? options = null);
    public async Task<string> GetResponseAsync(string message, OpenAI.Responses.ResponseReasoningOptions? options = null);
}
```

## Advanced Features

### Custom Function with Complex Parameters

```csharp
[Description("Add items to shopping cart")]
static CartResult AddToCart(
    [Description("Product name")] string product,
    [Description("Quantity to add")] int quantity = 1,
    [Description("Special requirements")] string[] requirements = null)
{
    return new CartResult
    {
        Success = true,
        ItemsInCart = 3,
        TotalPrice = 299.99m
    };
}

var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");
var chat = client.StartConversation("You are a shopping assistant.");

var result = chat.Send<CartResult>("Add 2 gaming laptops to my cart");
Console.WriteLine($"Cart now has {result.ItemsInCart} items, total: ${result.TotalPrice}");
```

### Error Handling

```csharp
try
{
    var result = chat.Send<MyType>("Hello", MyFunction);
    if (result != null)
    {
        // Use result
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// Or for simple string responses
var result = chat.Send<string>("Hello");
if (string.IsNullOrEmpty(result))
{
    Console.WriteLine("No response received");
}
```

## Best Practices

1. **Reuse Clients**: Create one client per API provider and reuse it
2. **Separate Conversations**: Use different conversations for different contexts
3. **Handle Nulls**: Check for null returns from Send calls
4. **Use Structured Output**: Define classes for complex response formats
5. **Leverage State**: Use conversation state to maintain context

## Logging

Enable logging to see API interactions:

```csharp
FluentClient.Logger = Console.Out;

var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");
// Now all API calls will be logged to console
```

## Installation

```bash
dotnet add package FluentAI.Net
```

## License

MIT License 