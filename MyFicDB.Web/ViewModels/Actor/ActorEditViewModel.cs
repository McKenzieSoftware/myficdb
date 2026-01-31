using System.ComponentModel.DataAnnotations;

namespace MyFicDB.Web.ViewModels.Actor
{
    public sealed class ActorEditViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = default!;

        public string Slug { get; set; } = default!;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Range(0, 99_999)]
        public int? Age { get; set; }

        // Existing image state
        public bool HasImage { get; set; }

        // Image changes
        public IFormFile? Image { get; set; }

        // If checked, deletes the current image (unless a new image is uploaded, in which case replace wins)
        public bool RemoveImage { get; set; }
    }
}
