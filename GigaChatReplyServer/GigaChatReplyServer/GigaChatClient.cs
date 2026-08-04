using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class GigaChatClient
{
    private readonly string _authKey;
    private readonly string _scope;
    private readonly HttpClient _httpClient;
    private string _accessToken;
    private DateTime _tokenExpiresAt;

    public GigaChatClient(string authorizationKey, string scope = "GIGACHAT_API_PERS")
    {
        _authKey = authorizationKey;
        _scope = scope;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
    }

    private async Task EnsureTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt)
            return;

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");

        request.Headers.Add("RqUID", Guid.NewGuid().ToString());
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", _authKey);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("scope", _scope)
        });
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        _accessToken = doc.RootElement.GetProperty("access_token").GetString();

        // обновляем заранее — за 2 минуты до истечения 30-минутного срока
        _tokenExpiresAt = DateTime.UtcNow.AddMinutes(28);
    }

    public async Task<string> ListModelsAsync()
    {
        await EnsureTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.giga.chat/v1/models");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GenerateReplyAsync(string model, string userMessage, string chatContext)
    {
        await EnsureTokenAsync();

        var payload = new
        {
            model = model,
            messages = new[]
            {
            new { role = "system", content = chatContext },
            new { role = "user", content = userMessage }
        }
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.giga.chat/v1/chat/completions");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var reply = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        // подстраховка на случай, если модель всё же вставит длинное тире
        reply = reply?.Replace('—', '-');

        return reply;
    }
}