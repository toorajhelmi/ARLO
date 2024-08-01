using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace Research.DiscArch.Services;

public class OpenAIEmbedding
{
    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("input")]
    public string[] Input { get; set; }
}

public class ChatMessage
{
    [JsonProperty("role")]
    public string Role { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }
}

public class Choice
{
    [JsonProperty("message")]
    public ChatMessage Data { get; set; }
}

public class OpenAIEmbeddingResponse
{
    [JsonProperty("data")]
    public List<EmbeddingData> Data { get; set; }
}

public class EmbeddingData
{
    [JsonProperty("embedding")]
    public List<double> Embedding { get; set; }
}

public class GptService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    public const int MaxWordPerCall = 5000; //8000 tokens and 0.7 word per token 

    public GptService()
    {
        client = new HttpClient();
        apiKey = Environment.GetEnvironmentVariable("GptApiKey");
    }

    public async Task<string> Call(string instruction, string ask)
    {
        int delay = 1000;
        int retryCount = 0;
        int maxRetries = 5;
        string apiUrl = "https://api.openai.com/v1/chat/completions";
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = instruction },
                new { role = "user", content = ask }
            }
        };

        string jsonRequestBody = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        while (retryCount < maxRetries)
        {
            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Wait and retry
                await Task.Delay(delay);
                delay *= 2; // Exponential backoff
                retryCount++;
            }
            else
            {
                Console.WriteLine("ChatGTP Error: " + response.ReasonPhrase);
                throw new Exception("Error calling OpenAI Chat API: " + response.ReasonPhrase);
            }
        }

        throw new Exception("Error calling OpenAI Chat API: Too many requests");
    }

    public async Task<List<List<double>>> GetEmbeddings(List<string> conditions)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        int batchSize = 50;  // Adjust the batch size according to your needs and token limits
        var embeddings = new List<List<double>>();

        for (int i = 0; i < conditions.Count; i += batchSize)
        {
            var batch = conditions.Skip(i).Take(batchSize).ToList();
            var embeddingRequest = new OpenAIEmbedding
            {
                Model = "text-embedding-ada-002",
                Input = batch.ToArray()
            };

            var content = new StringContent(JsonConvert.SerializeObject(embeddingRequest), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/embeddings", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {response.StatusCode}, Details: {errorResponse}");
                throw new Exception ("Could not generate embeddings");
            }
            else
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var embeddingResponse = JsonConvert.DeserializeObject<OpenAIEmbeddingResponse>(responseString);

                foreach (var data in embeddingResponse.Data)
                {
                    embeddings.Add(data.Embedding);
                }
            }
        }

        return embeddings;
    }
}
