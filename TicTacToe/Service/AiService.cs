using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly string _chatEndpoint = "https://router.huggingface.co/v1/chat/completions";
    private readonly string _model = "zai-org/GLM-4.7-Flash";

    public AiService(string apiToken)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiToken);
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(_chatEndpoint, content);
        var responseText = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";

        if (!response.IsSuccessStatusCode)
        {
            var snippet = responseText.Length > 500 ? responseText.Substring(0, 500) : responseText;
            return $"Error {(int)response.StatusCode} {response.ReasonPhrase}. Content-Type: {contentType}. Body(snippet): {snippet}";
        }

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0)
            {
                var first = choices[0];

                if (first.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString() ?? string.Empty;
                }

                // Some responses may include content directly under "choices[].text" or similar — attempt a couple fallbacks.
                if (first.TryGetProperty("text", out var textProp))
                    return textProp.GetString() ?? string.Empty;

                return first.ToString();
            }

            // fallback top-level content
            if (root.TryGetProperty("message", out var topMessage) &&
                topMessage.TryGetProperty("content", out var topContent))
            {
                return topContent.GetString() ?? string.Empty;
            }

            return root.ToString();
        }
        catch (JsonException)
        {
            var snippet = responseText.Length > 500 ? responseText.Substring(0, 500) : responseText;
            return $"Error: Invalid JSON response. Content-Type: {contentType}. Body(snippet): {snippet}";
        }
    }
}