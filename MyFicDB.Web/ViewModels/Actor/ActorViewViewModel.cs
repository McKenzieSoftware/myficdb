namespace MyFicDB.Web.ViewModels.Actor
{
    public sealed class ActorViewViewModel
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = default!;
        public string Slug { get; init; } = default!;
        public string? Description { get; init; }
        public int? Age { get; init; }

        public bool HasImage { get; init; }

        public List<ActorStoryListItemViewModel> Stories { get; set; } = new();
    }
}