using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models
{
    [Table("tblActorImages")]
    [PrimaryKey(nameof(ActorId))]
    public sealed class ActorImage
    {
        public Guid ActorId { get; set; }
        public Actor Actor { get; set; } = default!;

        public byte[] Data { get; set; } = Array.Empty<byte>();

        // PNG/JPEG/WEBP etc
        public string ContentType { get; set; } = "application/octet-stream";

        public string? FileName { get; set; }

        // Lelps caching / Duplicate detection
        public string? Sha256 { get; set; }
    }
}
