namespace GigaChatReplyServer.Infrastructure
{
    public interface IGigaChatClient
    {
        Task<string> GenerateReplyAsync(string userMessage, string systemContext, CancellationToken ct = default);
    }
}
