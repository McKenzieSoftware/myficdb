namespace MyFicDB.Web.ViewModels.Tag
{
    public sealed class TagViewViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public List<TagStoryListItemViewModel> Stories { get; set; } = new();
    }
}
