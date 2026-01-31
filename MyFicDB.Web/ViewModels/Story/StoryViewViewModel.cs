using System.ComponentModel.DataAnnotations;

namespace MyFicDB.Web.ViewModels.Story
{
    public sealed class StoryViewViewModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = default!;
        public string? Summary { get; init; }
        public string? Notes { get; init; }

        [Display(Name = "Own Work")]
        public bool IsOwnWork { get; init; } = true;

        [Display(Name = "AI Generated")]
        public bool IsAIGenerated { get; init; } = false;

        [Display(Name = "NSFW")]
        public bool IsNsfw { get; init; } = false;

        public int NutCounter { get; set; } = 0;
        public int ReadCounter { get; set; } = 0;

        public List<StoryDetailsChapterListItemViewModel> Chapters { get; set; } = new();
        public List<StoryDetailsStoryTagItemViewModel> StoryTags { get; init; } = new();
        public List<StoryDetailsStoryActorItemViewModel> StoryActors { get; init; } = new();
        public List<StoryDetailsStorySeriesItemViewModel> StorySeries { get; init; } = new();

        public DateTime CreatedDate { get; init; }
        public DateTime UpdatedDate { get; init; }
    }

    public sealed class StoryDetailsChapterListItemViewModel
    {
        public int ChapterNumber { get; init; }
        public string? Title { get; init; }
    }

    public sealed class StoryDetailsStoryTagItemViewModel
    {
        public string Name { get; init; } = default!;
        public string Slug { get; init; } = default!;
    }

    public sealed class StoryDetailsStoryActorItemViewModel
    {
        public string Name { get; init; } = default!;
        public string Slug { get; init; } = default!;
        public bool HasImage { get; init; }
    }

    public sealed class StoryDetailsStorySeriesItemViewModel
    {
        public string Name { get; init; } = default!;
        public string Slug { get; init; } = default!;
    }
}
