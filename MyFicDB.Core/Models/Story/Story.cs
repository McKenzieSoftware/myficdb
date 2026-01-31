using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models.Story
{
    [Table("tblStories")]
    [PrimaryKey(nameof(Id))]
    public sealed class Story : Base
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = default!;
        public string? Summary { get; set; }
        public string? Notes { get; set; }

        public bool IsOwnWork { get; set; } = true;
        public bool IsAIGenerated { get; set; } = false;
        public bool IsNsfw { get; set; } = false;

        public int NutCounter { get; set; } = 0;
        public int ReadCounter { get; set; } = 0;


        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

        public ICollection<StoryTag> StoryTags { get; set; } = new List<StoryTag>();
        public ICollection<StoryActor> StoryActors { get; set; } = new List<StoryActor>();
        public ICollection<StorySeries> StorySeries { get; set; } = new List<StorySeries>();
    }
}
