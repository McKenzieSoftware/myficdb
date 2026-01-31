namespace MyFicDB.Web.ViewModels.Actor
{
    public sealed class ActorIndexItemViewModel
    {
        public string Name { get; init; } = default!;
        public string Slug { get; init; } = default!;
        public bool HasImage { get; init; }
    }
}
