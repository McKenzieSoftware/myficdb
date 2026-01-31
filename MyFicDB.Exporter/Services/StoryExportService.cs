using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Models.Story;
using MyFicDB.Exporter.Enums;
using MyFicDB.Exporter.Interfaces;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace MyFicDB.Exporter.Services
{
    public sealed class StoryExportService : IStoryExportService
    {
        private readonly ApplicationDbContext _context;

        private readonly Dictionary<StoryExportType, Func<StoryAggregate, ExportPayload>> _renderers;

        private readonly ReverseMarkdown.Converter _htmlToMarkdownConverter;

        // Export recrods, do not modify
        private sealed record ExportPayload(byte[] Bytes, string ContentType, string Extension);
        private sealed record StoryAggregate(Guid Id, string Title, DateTimeOffset CreatedDate, List<string> Actors, List<string> Tags, string Summary, List<StoryChapterAggregate> Chapters);
        private sealed record StoryChapterAggregate(int ChapterNumber, string Title, string Body);

        public StoryExportService(ApplicationDbContext context)
        {
            _context = context;

            _htmlToMarkdownConverter = new ReverseMarkdown.Converter(new ReverseMarkdown.Config
            {
                GithubFlavored = true,
                RemoveComments = true,
                SmartHrefHandling = true
            });

            _renderers = new()
            {
                [StoryExportType.HTML] = RenderHtml,
                [StoryExportType.Markdown] = RenderMarkdown
            };
        }

        public async Task<FileContentResult> ExportAllStoriesAsHtmlZipAsync(CancellationToken cancellationToken = default)
        {
            var stories = await LoadAllStoriesAggregatesAsync(cancellationToken);

            if(stories is null)
            {
                throw new KeyNotFoundException("No stories found");
            }

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var story in stories)
                    {
                        var payload = RenderHtml(story);
                        var entryName = $"{story.Title}-{story.Id:N}.html";

                        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(payload.Bytes, cancellationToken);
                    }
                }

                var zipBytes = ms.ToArray();
                var zipName = $"myficdb-stories-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip";

                return new FileContentResult(zipBytes, "application/zip")
                {
                    FileDownloadName = zipName
                };
            }

        }

        public async Task<FileContentResult> ExportStoryAsync(Guid storyId, StoryExportType type, CancellationToken cancellationToken = default)
        {
            var story = await LoadStoryAggreateAsync(storyId, cancellationToken);

            if (story is null)
            {
                throw new KeyNotFoundException($"Story not found: {storyId}");
            }

            if(!_renderers.TryGetValue(type, out var renderer))
            {
                throw new NotSupportedException($"Export type not supported: {type}");
            }

            var payload = renderer(story);

            var fileName = $"{story.Title}-{story.Id:N}.{payload.Extension}";

            return new FileContentResult(payload.Bytes, payload.ContentType)
            {
                FileDownloadName = fileName
            };
        }

        /// <summary>
        /// Gets single story then runs MapToAggregate
        /// </summary>
        private async Task<StoryAggregate?> LoadStoryAggreateAsync(Guid storyId, CancellationToken cancellationToken)
        {
            var story = await _context.Stories
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Content) 
                .Include(s => s.StoryTags)
                    .ThenInclude(st => st.Tag)
                .Include(s => s.StoryActors)
                    .ThenInclude(sa => sa.Actor)
                .FirstOrDefaultAsync(s => s.Id == storyId, cancellationToken);

            if (story is null)
            {
                return null;
            }

            return MapToAggregate(story);
        }

        /// <summary>
        /// Gets all stories in the system then runs MapToAggregate
        /// </summary>
        private async Task<List<StoryAggregate>> LoadAllStoriesAggregatesAsync(CancellationToken cancellationToken)
        {
            var stories = await _context.Stories
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Content)
                .Include(s => s.StoryTags)
                    .ThenInclude(st => st.Tag)
                .Include(s => s.StoryActors)
                    .ThenInclude(sa => sa.Actor)
                .OrderBy(s => s.CreatedDate)
                .ToListAsync(cancellationToken);

            return stories.Select(MapToAggregate).ToList();
        }

        private static StoryAggregate MapToAggregate(Story story)
        {
            var chapters = story.Chapters
                .OrderBy(c => c.ChapterNumber)
                .Select(ch => new StoryChapterAggregate(
                    ChapterNumber: ch.ChapterNumber,
                    Title: ch.Title ?? string.Empty,
                    Body: ch.Content?.Body ?? string.Empty
                ))
                .ToList();

            var tags = story.StoryTags
                .Select(st => st.Tag.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var actors = story.StoryActors
                .Select(sa => sa.Actor.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var summary = story.Summary ?? string.Empty;

            return new StoryAggregate(
                Id: story.Id,
                Title: story.Title,
                CreatedDate: story.CreatedDate,
                Actors: actors,
                Tags: tags,
                Summary: summary,
                Chapters: chapters
            );
        }

        /// <summary>
        /// Render story as HTML, used when exporting to html
        /// </summary>
        private ExportPayload RenderHtml(StoryAggregate story)
        {
            var enc = HtmlEncoder.Default;

            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine($"<title>{enc.Encode(story.Title)}</title>");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;max-width:900px;margin:40px auto;padding:0 16px;line-height:1.6}");
            sb.AppendLine("header{border-bottom:1px solid #ddd;padding-bottom:16px;margin-bottom:24px}");
            sb.AppendLine(".meta{color:#444;font-size:0.95rem}");
            sb.AppendLine(".meta strong{color:#000}");
            sb.AppendLine("h1{line-height:1.2}");
            sb.AppendLine("section.chapter{margin-top:32px;padding-top:16px;border-top:1px solid #eee}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Top section
            sb.AppendLine("<header>");
            sb.AppendLine($"<h1>{enc.Encode(story.Title)}</h1>");
            sb.AppendLine("<div class=\"meta\">");
            sb.AppendLine($"<div><strong>Created:</strong> {enc.Encode(story.CreatedDate.ToString("yyyy-MM-dd HH:mm 'UTC'"))}</div>");
            sb.AppendLine($"<div><strong>Actors:</strong> {enc.Encode(story.Actors.Count == 0 ? "None" : string.Join(", ", story.Actors))}</div>");
            sb.AppendLine($"<div><strong>Tags:</strong> {enc.Encode(story.Tags.Count == 0 ? "None" : string.Join(", ", story.Tags))}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</header>");

            // Summary section
            sb.AppendLine("<section>");
            sb.AppendLine("<h2>Summary</h2>");
            sb.AppendLine(string.IsNullOrWhiteSpace(story.Summary) ? "<p><em>No summary.</em></p>" : story.Summary);
            sb.AppendLine("</section>");

            // Chapters
            foreach (var ch in story.Chapters)
            {
                sb.AppendLine("<section class=\"chapter\">");
                var chTitle = string.IsNullOrWhiteSpace(ch.Title)
                    ? $"Chapter {ch.ChapterNumber}"
                    : $"Chapter {ch.ChapterNumber}: {ch.Title}";

                sb.AppendLine($"<h2>{enc.Encode(chTitle)}</h2>");
                sb.AppendLine(string.IsNullOrWhiteSpace(ch.Body) ? "<p><em>No content.</em></p>" : ch.Body);
                sb.AppendLine("</section>");
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return new ExportPayload(bytes, "text/html; charset=utf-8", "html");
        }

        /// <summary>
        /// Render story as markdown, used when exporting to markdown/md
        /// </summary>
        private ExportPayload RenderMarkdown(StoryAggregate story)
        {
            var sb = new StringBuilder();

            // Top section
            sb.AppendLine($"# {story.Title}");
            sb.AppendLine();
            sb.AppendLine($"- **Created:** {story.CreatedDate:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine($"- **Actors:** {(story.Actors.Count == 0 ? "None" : string.Join(", ", story.Actors))}");
            sb.AppendLine($"- **Tags:** {(story.Tags.Count == 0 ? "None" : string.Join(", ", story.Tags))}");
            sb.AppendLine();

            // Summary
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(story.Summary) ? "_No summary._" : ConvertHtmlToMarkdown(story.Summary));
            sb.AppendLine();

            // Chapters
            foreach (var ch in story.Chapters)
            {
                var title = string.IsNullOrWhiteSpace(ch.Title) ? $"Chapter {ch.ChapterNumber}" : $"Chapter {ch.ChapterNumber}: {ch.Title}";

                sb.AppendLine($"## {title}");
                sb.AppendLine();

                sb.AppendLine(string.IsNullOrWhiteSpace(ch.Body) ? "_No content._" : ConvertHtmlToMarkdown(ch.Body));

                sb.AppendLine();
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return new ExportPayload(bytes, "text/markdown; charset=utf-8", "md");
        }

        private string ConvertHtmlToMarkdown(string html)
        {
            var md = _htmlToMarkdownConverter.Convert(html);

            // normalize excessive whitespace
            md = Regex.Replace(md, @"\n{3,}", "\n\n");
            return md.Trim();
        }

        /// <summary>
        /// Helper for controllers
        /// </summary>
        public bool TryParseType(string type, out StoryExportType exportType)
        {
            exportType = default;

            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            return type.Trim().ToLowerInvariant() switch
            {
                "html" => (exportType = StoryExportType.HTML) == StoryExportType.HTML,
                "markdown" => (exportType = StoryExportType.Markdown) == StoryExportType.Markdown,
                "md" => (exportType = StoryExportType.Markdown) == StoryExportType.Markdown,
                _ => false
            };
        }
    }
}
