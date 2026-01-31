namespace MyFicDB.Web.ViewModels.Story
{
    public sealed class StoryIndexPagedViewModel
    {
        public List<StoryCardViewModel> Stories { get; init; } = new();

        public int CurrentPage { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public int PageWindowSize { get; init; }

        public int WindowStart => ((CurrentPage - 1) / PageWindowSize) * PageWindowSize + 1;

        public int WindowEnd => Math.Min(WindowStart + PageWindowSize - 1, TotalPages);

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}
