
namespace MyFicDB.Web.ViewModels.Story
{
    public sealed record TagLinkRecord(string Name, string Slug);
    public sealed record SeriesLinkRecord(string Name, string Slug);

    public sealed class StoryCardViewModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = default!;
        public string? Summary { get; init; }
        public DateTimeOffset CreatedDate { get; init; }

        public IReadOnlyList<SeriesLinkRecord> Series { get; init; } = [];
        public IReadOnlyList<TagLinkRecord> Tags { get; init; } = [];

        public int TotalWordCount { get; init; }
        public int ChapterCount { get; init; }
        public int NutCount { get; init; }
        public int ReadCount { get; init; }

        public bool IsAIGenerated { get; init; }
        public bool IsOwnWork { get; init; }
        public bool IsNsfw { get; init; }
    }
}
