using GigaChatReplyServer.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GigaChatReplyServer.Infrastructure
{
    public class GigaChatClient: IGigaChatClient
    {
        private readonly GigaChatOptions _options;
        private readonly HttpClient _httpClient;
        private string? _accessToken;
        private DateTime _tokenExpiresAt;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        public GigaChatClient(IOptions<GigaChatOptions> options, HttpClient httpClient)
        {
            _options = options.Value;
            _httpClient = httpClient;
        }

        private async Task EnsureTokenAsync(CancellationToken ct)
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt)
                return;

            await _tokenLock.WaitAsync(ct);
            try
            {
                if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt)
                    return;

                var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");

                request.Headers.Add("RqUID", Guid.NewGuid().ToString());
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _options.AuthKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("scope", _options.Scope)
            });

                var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);
                _accessToken = doc.RootElement.GetProperty("access_token").GetString();
                _tokenExpiresAt = DateTime.UtcNow.AddMinutes(28);
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        public async Task<string> GenerateReplyAsync(string userMessage, string systemContext, CancellationToken ct = default)
        {
            await EnsureTokenAsync(ct);

            var payload = new
            {
                model = _options.Model,
                messages = new object[]
                {
                new { role = "system", content = systemContext },
                new { role = "user", content = userMessage }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.giga.chat/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var reply = doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString()
                ?? throw new GigaChatEmptyReplyException();

            return reply.Replace('—', '-');
        }
    }

    public class GigaChatEmptyReplyException : Exception
    {
        public GigaChatEmptyReplyException() : base("GigaChat returned an empty reply.") { }
    }
}
