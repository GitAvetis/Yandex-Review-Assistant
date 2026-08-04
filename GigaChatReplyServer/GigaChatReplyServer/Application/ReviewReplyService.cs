using GigaChatReplyServer.Infrastructure;
using GigaChatReplyServer.Options;
using Microsoft.Extensions.Options;

namespace GigaChatReplyServer.Application
{
    public class ReviewReplyService : IReviewReplyService
    {
        private readonly IGigaChatClient _client;
        private readonly string _chatContext;

        public ReviewReplyService(IGigaChatClient client, IOptions<GigaChatOptions> options, IHostEnvironment env)
        {
            _client = client;
            var path = Path.Combine(env.ContentRootPath, options.Value.ChatContextFile);
            _chatContext = File.ReadAllText(path);
        }

        public Task<string> GenerateReplyAsync(string reviewText, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reviewText))
                throw new ArgumentException("Review text cannot be empty.", nameof(reviewText));

            return _client.GenerateReplyAsync(reviewText, _chatContext, ct);
        }
    }
}
