using Microsoft.EntityFrameworkCore;
using MyFicDB.Core.Models.Story;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models
{
    [Table("tblActors")]
    [PrimaryKey(nameof(Id))]
    public sealed class Actor : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public string? Description { get; set; }
        public int? Age { get; set; }

        public ICollection<StoryActor> StoryActors { get; set; } = new List<StoryActor>();

        // Image is optional, doing this as a navigation prop. to prevent large reads
        // when we only want to pull actor names etc.
        public ActorImage? Image { get; set; }
    }
}
