using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Extensions;
using MyFicDB.Core.Models;
using MyFicDB.Web.ViewModels.Tag;

namespace MyFicDB.Web.Controllers
{
    [Authorize]
    [Route("tags")]
    public class TagsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TagsController> _logger;

        public TagsController(ApplicationDbContext context, ILogger<TagsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var tags = await _context.Tags
                .AsNoTracking()
                .Include(t => t.StoryTags)
                .ToListAsync();

            return View(tags);
        }

        [HttpGet("/tag/{slug}")]
        public async Task<IActionResult> View(string slug, CancellationToken cancellationToken)
        {
            var tag = await _context.Tags
                .AsNoTracking()
                .Where(t => t.Slug == slug)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Slug
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (tag is null)
            {
                return NotFound();
            }

            var stories = await _context.StoryTags
                .AsNoTracking()
                .Where(st => st.Tag.Slug == slug)
                .OrderByDescending(st => st.Story.CreatedDate)
                    .ThenBy(st => st.Story.Id)
                .Select(st => new TagStoryListItemViewModel
                {
                    StoryId = st.Story.Id,
                    Title = st.Story.Title,
                    Summary = st.Story.Summary,
                    ChapterCount = st.Story.Chapters.Count,
                    CreatedDate = st.Story.CreatedDate,
                })
                .ToListAsync(cancellationToken);

            var viewModel = new TagViewViewModel
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Stories = stories
            };

            return View(viewModel);
        }

        [HttpPost("/tag/{slug}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
        {
            var tag = await _context.Tags
                .AsNoTracking()
                .Where(t => t.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (tag is null)
            {
                return NotFound();
            }

            try
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync(cancellationToken);

                this.FlashSuccess($"Tag {tag.Name} deleted succesfully");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete Tag. Tag={slug}", slug);
                this.FlashError($"We couldn't delete the Tag {tag.Name} due to it being assigned to a Story, please remove the relationship and try again.");
                
                return RedirectToAction(nameof(View), new { slug });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting Tag. Tag={slug}", slug);
                this.FlashError("An unexpected error occurred, please try again.");

                return RedirectToAction(nameof(Index)); // redirect back to tags index
            }
        }
    }
}
