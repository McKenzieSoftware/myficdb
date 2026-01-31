using MyFicDB.Web.ViewModels.Story;

namespace MyFicDB.Web.ViewModels.GlobalSearch
{
    public sealed class GlobalSearchIndexViewModel
    {
        public List<StoryCardViewModel> Stories { get; init; } = new();
    }
}