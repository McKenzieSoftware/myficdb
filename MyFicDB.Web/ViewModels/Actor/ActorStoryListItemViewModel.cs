namespace MyFicDB.Web.ViewModels.Actor
{
    public sealed class ActorStoryListItemViewModel
    {
        public Guid StoryId { get; init; }
        public string Title { get; init; } = default!;
        public string? Summary { get; init; }
        public int ChapterCount { get; init; }
    }
}
