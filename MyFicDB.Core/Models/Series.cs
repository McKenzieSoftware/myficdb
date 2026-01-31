using Microsoft.EntityFrameworkCore;
using MyFicDB.Core.Models.Story;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models
{
    [Table("tblSeries")]
    [PrimaryKey(nameof(Id))]
    public sealed class Series : Base
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;

        public string NormalizedName { get; set; } = default!;

        public string Slug { get; set; } = default!;

        public ICollection<StorySeries> StorySeries { get; set; } = new List<StorySeries>();
    }
}
