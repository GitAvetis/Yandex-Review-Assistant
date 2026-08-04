namespace GigaChatReplyServer.Options
{
    public class GigaChatOptions
    {
        public const string SectionName = "GigaChat";

        public required string AuthKey { get; set; }
        public string Scope { get; set; } = "GIGACHAT_API_PERS";
        public string Model { get; set; } = "GigaChat-3-Ultra";
        public string ChatContextFile { get; set; } = "chatContext.txt";
    }
}
