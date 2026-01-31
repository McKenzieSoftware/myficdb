using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Extensions;
using MyFicDB.Core.Models;
using MyFicDB.Web.ViewModels.Series;

namespace MyFicDB.Web.Controllers
{
    [Authorize]
    [Route("series")]
    public sealed class SeriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SeriesController> _logger;

        public SeriesController(ApplicationDbContext context, ILogger<SeriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var series = await _context.Series
                .AsNoTracking()
                .Include(s => s.StorySeries)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return View(series);
        }

        [HttpGet("/series/{slug}")]
        public async Task<IActionResult> View(string slug, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var vm = await _context.Series
                .AsNoTracking()
                .Where(s => s.Slug == slug)
                .Select(s => new SeriesViewViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Slug = s.Slug
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (vm is null)
            {
                return NotFound();
            }

            vm.Stories = await _context.StorySeries
                .AsNoTracking()
                .Where(ss => ss.SeriesId == vm.Id)
                .Select(ss => ss.Story)
                .Distinct()
                .Select(story => new SeriesStoryListItemViewModel
                {
                    StoryId = story.Id,
                    Title = story.Title,
                    Summary = story.Summary,
                    ChapterCount = story.Chapters.Count,
                    CreatedDate = story.CreatedDate
                })
                .OrderByDescending(x => x.CreatedDate)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken);

            return View(vm);
        }
    
        public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
        {
            var series = await _context.Series
                .AsNoTracking()
                .Where(s => s.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (series is null)
            {
                return NotFound();
            }

            try
            {
                _context.Series.Remove(series);
                await _context.SaveChangesAsync(cancellationToken);

                this.FlashSuccess($"Series {series.Name} deleted succesfully");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete Series. Series={slug}", slug);
                this.FlashError($"We couldn't delete the Series {series.Name} due to it being assigned to a Story, please remove the relationship and try again.");

                return RedirectToAction(nameof(View), new { slug });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting Series. Series={slug}", slug);
                this.FlashError("An unexpected error occurred, please try again.");
                return RedirectToAction(nameof(Index)); // redirect back to Series index
            }
        }
    }
}
