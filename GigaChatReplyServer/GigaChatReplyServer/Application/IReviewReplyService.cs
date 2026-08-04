namespace GigaChatReplyServer.Application
{
    public interface IReviewReplyService
    {
        Task<string> GenerateReplyAsync(string reviewText, CancellationToken ct = default);
    }
}
