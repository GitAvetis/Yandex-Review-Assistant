using GigaChatReplyServer.Application;
using GigaChatReplyServer.Contracts;

namespace GigaChatReplyServer.Endpoints
{
    public static class ReviewEndpoints
    {
        public static void MapReviewEndpoints(this WebApplication app)
        {
            app.MapPost("/reply", async (ReviewRequest req, IReviewReplyService service, CancellationToken ct) =>
            {
                var reply = await service.GenerateReplyAsync(req.Text, ct);
                return Results.Ok(new ReviewReplyResponse(reply));
            });
        }
    }
}
