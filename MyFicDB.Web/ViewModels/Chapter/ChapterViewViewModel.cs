namespace MyFicDB.Web.ViewModels.Chapter
{
    public sealed class ChapterViewViewModel
    {
        public Guid StoryId { get; set; }
        public Guid ChapterId { get; set; }

        public int ChapterNumber { get; set; }
        public string? Title { get; set; }

        public string Body { get; set; } = string.Empty;

        public int WordCount { get; set; } = 0;

        public TimeSpan TimeToRead { get; set; }

        public int? PreviousChapterNumber { get; set; }
        public int? NextChapterNumber { get; set; }
    }
}
