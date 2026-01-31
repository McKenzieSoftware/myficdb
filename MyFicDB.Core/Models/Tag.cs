using Microsoft.EntityFrameworkCore;
using MyFicDB.Core.Models.Story;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models
{
    [Table("tblTags")]
    [PrimaryKey(nameof(Id))]
    public sealed class Tag : Base
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;

        public string NormalizedName { get; set; } = default!;

        public string Slug { get; set; } = default!;

        public ICollection<StoryTag> StoryTags { get; set; } = new List<StoryTag>();
    }
}
