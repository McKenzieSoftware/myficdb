using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Extensions;
using MyFicDB.Core.Models;
using MyFicDB.Web.Services;
using MyFicDB.Web.ViewModels.Chapter;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MyFicDB.Web.Controllers
{
    [Authorize]
    [Route("story/{storyId:guid}/chapter")]
    public sealed class ChaptersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChaptersController> _logger;
        private readonly HtmlSanitizationService _htmlSanitizationService;

        public ChaptersController(ApplicationDbContext context, ILogger<ChaptersController> logger, HtmlSanitizationService htmlSanitizationService)
        {
            _context = context;
            _logger = logger;
            _htmlSanitizationService = htmlSanitizationService;
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create(Guid storyId)
        {
            var story = await _context.Stories
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == storyId);

            if (story is null)
            {
                return NotFound();
            }

            var nextChapter = await _context.Chapters
                .Where(c => c.StoryId == storyId)
                .Select(c => (int?)c.ChapterNumber)
                .MaxAsync();

            var viewModel = new ChapterCreateViewModel
            {
                StoryId = storyId,
                StoryTitle = story.Title,
                ChapterNumber = (nextChapter ?? 0) + 1
            };

            return View(viewModel);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create (Guid storyId, ChapterCreateViewModel model)
        {
            if (storyId != model.StoryId)
            {
                return BadRequest();
            }

            var story = await _context.Stories
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == storyId);

            if (story is null)
            {
                return NotFound();
            }

            // If validation fails
            if(!ModelState.IsValid)
            {
                model.StoryTitle = story.Title;
                return View(model);
            }

            // Enforce chapter number uniquieness
            var exists = await _context.Chapters
                .AnyAsync(c => c.StoryId == storyId && c.ChapterNumber == model.ChapterNumber);

            if(exists)
            {
                ModelState.AddModelError(nameof(model.ChapterNumber), "That chapter number already exists.");
                model.StoryTitle = story.Title;
                return View(model);
            }

            // Normalize
            var bodyHtml = (model.Body ?? string.Empty).Trim();
            if(string.IsNullOrWhiteSpace(bodyHtml))
            {
                ModelState.AddModelError(nameof(model.Body), "Chapter body is required.");
                model.StoryTitle = story.Title;
                return View(model);
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            var chapterId = Guid.NewGuid();

            var chapter = new Chapter
            {
                Id = chapterId,
                StoryId = storyId,
                ChapterNumber = model.ChapterNumber,
                Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim()
            };

            var content = new ChapterContent
            {
                ChapterId = chapterId,
                Body = _htmlSanitizationService.Sanitize(bodyHtml)
            };

            // Add word count
            content.WordCount = CountWords(content.Body);

            // Set nav props
            chapter.Content = content;
            content.Chapter = chapter;

            _context.Add(chapter);
            _context.Add(content);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return RedirectToAction("View", "Stories", new { id = storyId });
        }

        [HttpGet("{chapterNumber:int}")]
        public async Task<IActionResult> View(Guid storyId, int chapterNumber)
        {
            // Load current chapter + content
            var chapter = await _context.Chapters
                .AsNoTracking()
                .Include(c => c.Content)
                .FirstOrDefaultAsync(c => c.StoryId == storyId && c.ChapterNumber == chapterNumber);

            if (chapter is null)
            {
                return NotFound();
            }

            // Determine previous and next chapter numbers (within the same story)
            var prevNumber = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.StoryId == storyId && c.ChapterNumber < chapterNumber)
                .OrderByDescending(c => c.ChapterNumber)
                .Select(c => (int?)c.ChapterNumber)
                .FirstOrDefaultAsync();

            var nextNumber = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.StoryId == storyId && c.ChapterNumber > chapterNumber)
                .OrderBy(c => c.ChapterNumber)
                .Select(c => (int?)c.ChapterNumber)
                .FirstOrDefaultAsync();

            int wordCount = 0;
            TimeSpan timeToRead = TimeSpan.Zero;

            if(chapter.Content is not null)
            {
                wordCount = chapter.Content.WordCount;
                timeToRead = chapter.Content.TimeToRead;
            }

            var vm = new ChapterViewViewModel
            {
                StoryId = storyId,
                ChapterId = chapter.Id,
                ChapterNumber = chapter.ChapterNumber,
                Title = chapter.Title,
                Body = chapter.Content?.Body ?? string.Empty,
                PreviousChapterNumber = prevNumber,
                NextChapterNumber = nextNumber,
                WordCount = wordCount,
                TimeToRead = timeToRead,
            };

            return View(vm);
        }

        [HttpGet("{chapterNumber:int}/edit")]
        public async Task<IActionResult> Edit(Guid storyId, int chapterNumber)
        {
            var story = await _context.Stories
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == storyId);

            if (story is null)
            {
                return NotFound();
            }

            var chapter = await _context.Chapters
                .AsNoTracking()
                .Include(c => c.Content)
                .FirstOrDefaultAsync(c => c.StoryId == storyId && c.ChapterNumber == chapterNumber);

            if (chapter is null)
            {
                return NotFound();
            }

            var inlineNotes = await _context.ChapterInlineNotes
                .AsNoTracking()
                .Where(n => n.ChapterId == chapter.Id)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new ChapterInlineNoteResponse
                {
                    Id = n.Id,
                    Details = n.Details,
                    CreatedDate = n.CreatedDate
                })
                .ToListAsync();

            var vm = new ChapterEditViewModel
            {
                StoryId = storyId,
                StoryTitle = story.Title,
                ChapterId = chapter.Id,
                ChapterNumber = chapter.ChapterNumber,
                Title = chapter.Title,
                Body = chapter.Content?.Body ?? string.Empty,
                InlineNotes = inlineNotes,
            };

            return View(vm);
        }

        [HttpPost("{chapterNumber:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid storyId, int chapterNumber, ChapterEditViewModel model)
        {
            if (storyId != model.StoryId)
            {
                return BadRequest();
            }

            var story = await _context.Stories
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == storyId);

            if (story is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.StoryTitle = story.Title;
                return View(model);
            }

            // Load the chapter by id (safe even if ChapterNumber changes)
            var chapter = await _context.Chapters
                .Include(c => c.Content)
                .FirstOrDefaultAsync(c => c.Id == model.ChapterId && c.StoryId == storyId);

            if (chapter is null)
            {
                return NotFound();
            }

            // Enforce chapter number uniqueness excluding current chapter
            var numberTaken = await _context.Chapters
                .AnyAsync(c => c.StoryId == storyId
                               && c.ChapterNumber == model.ChapterNumber
                               && c.Id != model.ChapterId);

            if (numberTaken)
            {
                ModelState.AddModelError(nameof(model.ChapterNumber), "That chapter number already exists.");
                model.StoryTitle = story.Title;
                return View(model);
            }

            // Normalize and sanitize body
            var bodyHtml = (model.Body ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(bodyHtml))
            {
                ModelState.AddModelError(nameof(model.Body), "Chapter body is required.");
                model.StoryTitle = story.Title;
                return View(model);
            }

            try
            {
                chapter.ChapterNumber = model.ChapterNumber;
                chapter.Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim();

                if (chapter.Content is null)
                {
                    // Safety: should not happen, but handle it
                    chapter.Content = new ChapterContent
                    {
                        ChapterId = chapter.Id,
                        Chapter = chapter,
                        Body = _htmlSanitizationService.Sanitize(bodyHtml),
                    };
                }
                else
                {
                    chapter.Content.Body = _htmlSanitizationService.Sanitize(bodyHtml);
                }

                chapter.Content.WordCount = CountWords(chapter.Content.Body);

                await _context.SaveChangesAsync();

                // remove any inline notes that exist
                try
                {
                    var notes = await _context.ChapterInlineNotes
                    .Where(n => n.ChapterId == chapter.Id)
                    .ToListAsync();

                    if (notes.Count > 0)
                    {
                        _context.ChapterInlineNotes.RemoveRange(notes);
                        await _context.SaveChangesAsync();
                    }
                } catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to delete inline notes. StoryId={StoryId} ChapterId={ChapterId}", storyId, model.ChapterId);
                }

                this.FlashSuccess("Chapter updated.");
                return RedirectToAction("View", "Stories", new { id = storyId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to update chapter. StoryId={StoryId} ChapterId={ChapterId}", storyId, model.ChapterId);
                this.FlashError("We couldn't save your changes. Please try again.");
                model.StoryTitle = story.Title;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating chapter. StoryId={StoryId} ChapterId={ChapterId}", storyId, model.ChapterId);
                this.FlashError("An unexpected error occurred. Please try again.");
                model.StoryTitle = story.Title;
                return View(model);
            }
        }

        [HttpPost("{id:guid}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid storyId, Guid id)
        {
            try
            {
                var chapter = await _context.Chapters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && c.StoryId == storyId);

                if (chapter is null)
                {
                    return NotFound();
                }

                // prevents us reattaching the tracked instance
                _context.Chapters.Remove(new Chapter { Id = id });
                await _context.SaveChangesAsync();

                this.FlashSuccess("Chapter deleted.");
                return RedirectToAction("View", "Stories", new { id = storyId }); // redirect back to the story
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete chapter. StoryId={StoryId}, ChapterId={ChapterId}", storyId, id);
                this.FlashError("We couldn't delete that story, please try again.");
                return RedirectToAction("View", "Stories", new { id = storyId }); // redirect back to the story
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting chapter. StoryId={StoryId}, ChapterId={ChapterId}", storyId, id);
                this.FlashError("An unexpected error occurred, please try again.");
                return RedirectToAction("View", "Stories", new { id = storyId }); // redirect back to the story
            }
        }

        [HttpGet("{chapterNumber:int}/notes")]
        public async Task<IActionResult> GetNotes(Guid storyId, int chapterNumber)
        {
            var chapterId = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.StoryId == storyId && c.ChapterNumber == chapterNumber)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();

            if (chapterId is null)
            {
                return NotFound();
            }

            var notes = await _context.ChapterInlineNotes
                .AsNoTracking()
                .Where(n => n.ChapterId == chapterId.Value)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new ChapterInlineNoteResponse
                {
                    Id = n.Id,
                    Details = n.Details,
                    CreatedDate = n.CreatedDate
                })
                .ToListAsync();

            return Ok(notes);
        }

        [HttpPost("{chapterNumber:int}/notes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNote(Guid storyId, int chapterNumber, [FromBody] ChapterCreateInlineNoteRequest req)
        {
            var details = (req.Details ?? string.Empty).Trim();
            if (details.Length < 2)
            {
                return BadRequest("Note details is too short.");
            }

            if (details.Length > 800)
            {
                details = details[..800];
            }

            var chapter = await _context.Chapters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.StoryId == storyId && c.ChapterNumber == chapterNumber);

            if (chapter is null)
            {
                return NotFound();
            }

            var note = new ChapterInlineNote
            {
                ChapterId = chapter.Id,
                Details = details
            };

            _context.ChapterInlineNotes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(new ChapterInlineNoteResponse
            {
                Id = note.Id,
                Details = note.Details,
                CreatedDate = note.CreatedDate
            });
        }

        [HttpDelete("{chapterNumber:int}/notes/{noteId:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(Guid storyId, int chapterNumber, Guid noteId)
        {
            var chapterId = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.StoryId == storyId && c.ChapterNumber == chapterNumber)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();

            if (chapterId is null)
            {
                return NotFound();
            }

            var note = await _context.ChapterInlineNotes
                .FirstOrDefaultAsync(n => n.Id == noteId && n.ChapterId == chapterId.Value);

            if (note is null)
            {
                return NotFound();
            }

            _context.ChapterInlineNotes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // helpers

        private static int CountWords(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return 0;
            }

            // Body is HTML, remove tags before counting.
            var text = Regex.Replace(html, "<.*?>", " ");
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length;
        }

    }
}
