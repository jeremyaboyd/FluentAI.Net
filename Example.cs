using FluentAI.Net;

namespace FluentAI.Net.Examples
{
    /// <summary>
    /// Example demonstrating the new FluentAI.Net API
    /// </summary>
    public class FluentApiExample
    {
        public static async Task RunExamples()
        {
            // OLD API (DreamEngine):
            // var engine = Engine.CreateOpenAI("api-key", "gpt-4");
            // var dream = engine.CreateDream("You are a helpful assistant.");
            // var response = dream.PushFragment<string>("Hello, world!");

            // NEW API (FluentAI.Net):
            var client = FluentClient.UseOpenAI("your-api-key", "gpt-4");
            var chat = client.StartConversation("You are a helpful assistant.");
            var response = await chat.SendAsync<string>("Hello, world!");

            Console.WriteLine($"AI Response: {response}");

            // Example with function calling
            var weatherClient = FluentClient.UseOpenAI("your-api-key", "gpt-4");
            var weatherChat = weatherClient.StartConversation("You are a weather assistant with access to real-time weather data.");
            
            var weatherResponse = await weatherChat.SendAsync<string>(
                "What's the weather like in London?", 
                GetWeather
            );

            Console.WriteLine($"Weather Response: {weatherResponse}");

            // Example with structured output
            var structuredClient = FluentClient.UseOpenAI("your-api-key", "gpt-4");
            var structuredChat = structuredClient.StartConversation("You are a weather service API.");
            
            var weatherData = await structuredChat.SendAsync<WeatherResponse>(
                "Get the weather for Paris",
                GetWeather
            );

            Console.WriteLine($"Weather in {weatherData?.City}: {weatherData?.Temperature}°C, {weatherData?.Condition}");
        }

        [Description("Gets current weather information for a specific city")]
        public static WeatherResponse GetWeather(
            [Description("The name of the city")] string city,
            [Description("Temperature unit (celsius or fahrenheit)")] string unit = "celsius")
        {
            // Simulate weather data
            return new WeatherResponse
            {
                City = city,
                Temperature = 22,
                Condition = "Sunny"
            };
        }
    }

    public class WeatherResponse
    {
        public string City { get; set; } = "";
        public int Temperature { get; set; }
        public string Condition { get; set; } = "";
    }
} 