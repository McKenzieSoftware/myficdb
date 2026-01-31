using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFicDB.Core.Models
{
    [Table("tblChapters")]
    [PrimaryKey(nameof(Id))]
    public sealed class Chapter : Base
    {
        public Guid Id { get; set; }

        public Guid StoryId { get; set; }
        public Story.Story Story { get; set; } = default!;

        public int ChapterNumber { get; set; }
        public string? Title { get; set; }

        public ChapterContent Content { get; set; } = default!;
    }

    [Table("tblChapterContents")]
    [PrimaryKey(nameof(ChapterId))]
    public sealed class ChapterContent
    {
        public Guid ChapterId { get; set; }

        public Chapter Chapter { get; set; } = default!;

        public string Body { get; set; } = default!;

        public int WordCount { get; set; }

        [NotMapped]
        public TimeSpan TimeToRead => WordCount == 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(WordCount / 300.0);
    }

    [Table("tblChapterInlineNote")]
    [PrimaryKey(nameof(Id))]
    public sealed class ChapterInlineNote : Base
    {
        public Guid Id { get; set; }

        public Guid ChapterId { get; set; }

        public Chapter Chapter { get; set; } = default!;

        [Required, MaxLength(800)]
        public string Details { get; set; } = default!;
    }

    public sealed class ChapterCreateInlineNoteRequest
    {
        public string Details { get; set; } = string.Empty;
    }

    public sealed class ChapterInlineNoteResponse
    {
        public Guid Id { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTimeOffset CreatedDate { get; set; }
    }
}
