namespace MyFicDB.Web.ViewModels.Tag
{
    public sealed class TagStoryListItemViewModel
    {
        public Guid StoryId { get; init; }
        public string Title { get; init; } = default!;
        public string? Summary { get; init; }
        public int ChapterCount { get; init; }
        public DateTime CreatedDate { get; set; }
    }
}
