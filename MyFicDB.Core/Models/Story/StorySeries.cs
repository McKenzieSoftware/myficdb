using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models.Story
{
    [Table("tblStorySeries")]
    public sealed class StorySeries
    {
        public Guid StoryId { get; set; }
        public Story Story { get; set; } = default!;

        public Guid SeriesId { get; set; }
        public Series Series { get; set; } = default!;
    }
}
