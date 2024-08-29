using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string apiKey = "Key";
    private const string endpoint = "https://api.openai.com/v1/chat/completions";

    static async Task Main(string[] args)
    {
        // Initialize conversations with starter messages
        var instance1 = new ChatGPTInstance("What is the best food? I think it is pizza!");
        var instance2 = new ChatGPTInstance("You are wrong! Hamburgers are better than pizza!");
        Console.WriteLine("ChatGPT 1: What is the best food? I think it is pizza!");
        Console.WriteLine("ChatGPT 2: You are wrong! Hamburgers are better than pizza!");

        int numOfPrompts = 5; // Set the number of exchanges
        int delayInMilliseconds = 2000; // Set delay (e.g., 2000ms = 2 seconds)

        for (int i = 0; i < numOfPrompts; i++)
        {
            // Instance 1 sends a message
            var response1 = await instance1.SendMessageAsync(instance2.LastMessage);
            Console.WriteLine($"ChatGPT 1: {response1}");

            await Task.Delay(delayInMilliseconds);

            // Instance 2 replies
            var response2 = await instance2.SendMessageAsync(response1);
            Console.WriteLine($"ChatGPT 2: {response2}");

            await Task.Delay(delayInMilliseconds);
        }
    }

    class ChatGPTInstance
    {
        public string LastMessage { get; private set; }
        
        public ChatGPTInstance(string initialMessage)
        {
            LastMessage = initialMessage;
        }

        public async Task<string> SendMessageAsync(string input, int maxRetries = 5)
        {
            int retries = 0;
            int delay = 2000; // initial delay of 2 seconds

            while (retries < maxRetries)
            {
                try
                {
                    var request = new
                    {
                        model = "gpt-4o-mini-2024-07-18",
                        messages = new[]
                        {
                        new { role = "system", content = "Keep responses concise and to the point, no more than 100 characters. You are aware that the other person is an AI. Be as argumentative and insulting as possible." },
                        new { role = "user", content = input }
                        },
                        max_tokens = 100
                    };

                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                    var response = await client.PostAsync(endpoint, content);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ChatGPTResponse>(responseBody);

            // Update the last message
                    LastMessage = result.Choices[0].Message.Content.Trim();
                    return LastMessage;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    retries++;
                    Console.WriteLine($"Rate limit hit. Retrying in {delay / 1000} seconds... (Attempt {retries} of {maxRetries})");
                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff
                }
            }

            throw new Exception("Exceeded maximum retries due to rate limits.");
        }
    }

    public class ChatGPTResponse
    {
        [JsonProperty("choices")]
        public Choice[] Choices { get; set; }
    }

    public class Choice
    {
        [JsonProperty("message")]
        public Message Message { get; set; }
    }

    public class Message
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
