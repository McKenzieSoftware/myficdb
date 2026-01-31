using MyFicDB.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyFicDB.Web.ViewModels.Story
{
    public sealed class StoryCreateViewModel
    {
        [Required]
        [StringLength(50)]
        public string Title { get; set; } = string.Empty;

        public string? Summary { get; set; }
        public string? Notes { get; set; }

        [Display(Name = "Own Work")]
        public bool IsOwnWork { get; set; } = true;

        [Display(Name = "AI Generated")]
        public bool IsAIGenerated { get; set; } = false;

        [Display(Name = "NSFW")]
        public bool IsNsfw { get; set; } = false;

        [CsvList(MaxItems = 30, MaxTokenLength = 50, MaxRawLength = 2000)]
        public string? Tags { get; set; }

        [CsvList(MaxItems = 30, MaxTokenLength = 50, MaxRawLength = 2000)]
        public string? Series { get; set; }

        [CsvList(MaxItems = 30, MaxTokenLength = 50, MaxRawLength = 2000)]
        public string? Actors { get; set; }
    }
}


