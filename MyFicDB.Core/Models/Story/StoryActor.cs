using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models.Story
{
    [Table("tblStoryActors")]
    public sealed class StoryActor
    {
        public Guid StoryId { get; set; }
        public Story Story { get; set; } = default!;

        public Guid ActorId { get; set; }
        public Actor Actor { get; set; } = default!;
    }
}
