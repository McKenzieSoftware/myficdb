using System.ComponentModel.DataAnnotations;

namespace MyFicDB.Web.ViewModels.Actor
{
    public sealed class ActorCreateViewModel
    {
        [Required, StringLength(50)]
        public string Name { get; set; } = default!;

        [StringLength(2000)]
        public string? Description { get; set; } = default!;

        [Range(0, 99999)]
        public int? Age { get; set; } = 18;

        // Optional image upload
        public IFormFile? Image { get; set; }
    }
}
