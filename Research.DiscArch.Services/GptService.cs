using System.Text;
using System.Text.Json;

namespace Research.DiscArch.Services;

public class GptService
{
    private readonly HttpClient client;
    private readonly string apiKey;

    public GptService()
    {
        client = new HttpClient();
        apiKey = Environment.GetEnvironmentVariable("GptApiKey");
    }

    public async Task<string> Call(string instruction, string ask)
    {
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

        string jsonRequestBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(apiUrl, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        else
        {
            Console.WriteLine("ChatGTP Error: " + response.ReasonPhrase);
            throw new Exception("Error calling OpenAI Chat API");
        }
    }
}
