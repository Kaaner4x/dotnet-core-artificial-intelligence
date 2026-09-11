using System.Text;
using System.Text.Json;
using DotNetEnv;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Automatically search and load the .env file
        Env.TraversePath().Load();

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        // If the environment variable is not set, prompt the user in the console
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: 'GEMINI_API_KEY' environment variable not found.");
            Console.Write("Please enter your Gemini API key: ");
            Console.ResetColor();

            apiKey = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No API key provided. Terminating application.");
                Console.ResetColor();
                return;
            }
            Console.Clear();
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Gemini AI Chatbot Started ===");
        Console.WriteLine("(Type 'exit' or 'quit' to end the conversation)\n");
        Console.ResetColor();

        using var httpClient = new HttpClient();
        var model = "gemini-flash-latest";

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("You: ");
            Console.ResetColor();

            var prompt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(prompt))
                continue;

            if (prompt.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                prompt.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var response = await httpClient.PostAsync(endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseString);
                    var answer = result.GetProperty("candidates")[0]
                                       .GetProperty("content")
                                       .GetProperty("parts")[0]
                                       .GetProperty("text")
                                       .GetString();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"\nGemini: {answer?.Trim()}\n");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nAn error occurred (Status: {response.StatusCode}):");
                    Console.WriteLine(responseString + "\n");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nConnection error: {ex.Message}\n");
                Console.ResetColor();
            }
        }
    }
}