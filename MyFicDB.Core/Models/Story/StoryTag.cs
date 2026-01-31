using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models.Story
{
    [Table("tblStoryTags")]
    public sealed class StoryTag
    {
        public Guid StoryId { get; set; }
        public Story Story { get; set; } = default!;

        public Guid TagId { get; set; }
        public Tag Tag { get; set; } = default!;
    }
}
