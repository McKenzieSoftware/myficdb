using System.ComponentModel.DataAnnotations;

namespace MyFicDB.Web.ViewModels.Chapter
{
    public sealed class ChapterCreateViewModel
    {
        [Required]
        public Guid StoryId { get; set; }

        public string StoryTitle { get; set; } = string.Empty;

        [Required]
        [Range(1, 10_000)]
        [Display(Name = "Chapter number")]
        public int ChapterNumber { get; set; }

        [StringLength(50)]
        public string? Title { get; set; }

        [Required]
        [Display(Name = "Chapter Body")]
        public string Body { get; set; } = string.Empty;
    }

}