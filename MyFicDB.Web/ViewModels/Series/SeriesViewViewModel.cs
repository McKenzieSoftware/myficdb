namespace MyFicDB.Web.ViewModels.Series
{
    public sealed class SeriesViewViewModel
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public string Slug { get; init; } = default!;

        public List<SeriesStoryListItemViewModel> Stories { get; set; } = new();
    }
}
