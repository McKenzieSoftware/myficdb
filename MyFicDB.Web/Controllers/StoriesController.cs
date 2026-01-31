using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Extensions;
using MyFicDB.Core.Models.Story;
using MyFicDB.Exporter.Interfaces;
using MyFicDB.Web.Services;
using MyFicDB.Web.ViewModels.Story;

namespace MyFicDB.Web.Controllers
{
    [Authorize]
    [Route("stories")]
    public sealed class StoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StoriesController> _logger;

        private readonly StoryRelationshipService _storyRelationshipService;
        private readonly IStoryExportService _storyExportService;

        public sealed class NutReadDeltaRequest
        {
            public int Delta { get; set; }
        }

        public StoriesController(ApplicationDbContext context, ILogger<StoriesController> logger, StoryRelationshipService storyRelationshipService, IStoryExportService storyExportService)
        {
            _context = context;
            _logger = logger;
            _storyRelationshipService = storyRelationshipService;
            _storyExportService = storyExportService;
        }

        [HttpGet("")]
        [HttpGet("{page:int}")]
        public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
        {
            const int pageSize = 10;
            const int pageWindowSize = 5;

            if (page < 1)
            {
                page = 1;
            }

            var totalStories = await _context.Stories.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalStories / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var stories = await _context.Stories
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedDate)
                    .ThenBy(s => s.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StoryCardViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    CreatedDate = s.CreatedDate,
                    Summary = s.Summary,

                    IsOwnWork = s.IsOwnWork,
                    IsAIGenerated = s.IsAIGenerated,
                    IsNsfw = s.IsNsfw,

                    ChapterCount = s.Chapters.Count,

                    TotalWordCount = s.Chapters
                        .Select(c => c.Content.WordCount)
                        .Sum(),

                    NutCount = s.NutCounter,
                    ReadCount = s.ReadCounter,

                    Tags = s.StoryTags
                        .OrderBy(st => st.Tag.Name)
                        .Select(st => new TagLinkRecord(st.Tag.Name, st.Tag.Slug))
                        .ToList(),

                    Series = s.StorySeries
                        .OrderBy(ss => ss.Series.Name)
                        .Select(ss => new SeriesLinkRecord(ss.Series.Name, ss.Series.Slug))
                        .ToList(),
                })
                .ToListAsync(cancellationToken);

            var vm = new StoryIndexPagedViewModel
            {
                Stories = stories,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                PageWindowSize = pageWindowSize
            };

            return View(vm);
        }

        [HttpGet("/story/create")]
        public IActionResult Create()
        {
            return View(new StoryCreateViewModel());
        }

        [HttpPost("/story/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StoryCreateViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var story = new Story
                {
                    Title = model.Title.Trim(),
                    Summary = string.IsNullOrWhiteSpace(model.Summary) ? null : model.Summary,
                    Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes,
                    IsAIGenerated = model.IsAIGenerated,
                    IsOwnWork = model.IsOwnWork,
                    IsNsfw = model.IsNsfw
                };

                _context.Stories.Add(story);

                await _storyRelationshipService.ApplyAsync(
                    story,
                    tagsCsv: model.Tags,
                    seriesCsv: model.Series,
                    actorsCsv: model.Actors,
                    cancellationToken: cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(View), new { id = story.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to create story. Title={Title}", model.Title);
                ModelState.AddModelError(string.Empty, "We couldn't save your story. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating story. Title={Title}", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpGet("/story/{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var story = await _context.Stories
                .AsNoTracking()
                .Include(s => s.StoryTags)
                    .ThenInclude(st => st.Tag)
                .Include(ss => ss.StorySeries)
                    .ThenInclude(ss => ss.Series)
                .Include(sa => sa.StoryActors)
                    .ThenInclude(sa => sa.Actor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if(story is null)
            {
                return NotFound();
            }

            var viewModel = new StoryEditViewModel
            {
                Id = story.Id,
                Title = story.Title,
                Summary = story.Summary,
                Notes = story.Notes,
                IsAIGenerated = story.IsAIGenerated,
                IsOwnWork = story.IsOwnWork,
                IsNsfw = story.IsNsfw,
                Tags = string.Join(", ", story.StoryTags
                                .Select(st => st.Tag.Name)
                                .OrderBy(n => n)),
                Series = string.Join(", ", story.StorySeries
                                .Select(ss => ss.Series.Name)
                                .OrderBy(n => n)),
                Actors = string.Join(", ", story.StoryActors
                                .Select(sa => sa.Actor.Name)
                                .OrderBy(n => n))

            };

            return View(viewModel);
        }

        [HttpPost("/story/{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, StoryEditViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var story = await _context.Stories
                    .Include(st => st.StoryTags)
                    .Include(ss => ss.StorySeries)
                    .Include(sa => sa.StoryActors)
                    .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

                if (story is null)
                {
                    return NotFound();
                }

                story.Title = model.Title.Trim();
                story.Summary = string.IsNullOrWhiteSpace(model.Summary) ? null : model.Summary;
                story.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes;
                story.IsAIGenerated = model.IsAIGenerated;
                story.IsNsfw = model.IsNsfw;
                story.IsOwnWork = model.IsOwnWork;

                await _storyRelationshipService.ApplyAsync(
                    story,
                    tagsCsv: model.Tags,
                    seriesCsv: model.Series,
                    actorsCsv: model.Actors,
                    cancellationToken: cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(View), new { id = story.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to edit story. Title={Title}", model.Title);
                ModelState.AddModelError(string.Empty, "We couldn't save your story. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing story. Title={Title}", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpGet("/story/{id:guid}")]
        public async Task<IActionResult> View(Guid id, CancellationToken cancellationToken)
        {
            var vm = await _context.Stories
                .AsNoTracking()
                .AsSplitQuery()
                .Where(s => s.Id == id)
                .Select(s => new StoryViewViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Summary = s.Summary,
                    Notes = s.Notes,
                    IsAIGenerated = s.IsAIGenerated,
                    IsNsfw = s.IsNsfw,
                    IsOwnWork = s.IsOwnWork,
                    CreatedDate = s.CreatedDate,
                    UpdatedDate = s.UpdatedDate,
                    NutCounter = s.NutCounter,
                    ReadCounter = s.ReadCounter,

                    Chapters = s.Chapters
                        .Select(c => new StoryDetailsChapterListItemViewModel
                        {
                            ChapterNumber = c.ChapterNumber,
                            Title = c.Title
                        })
                        .ToList(),

                    StoryTags = s.StoryTags
                        .Select(st => new StoryDetailsStoryTagItemViewModel
                        {
                            Name = st.Tag.Name,
                            Slug = st.Tag.Slug
                        })
                        .ToList(),

                    StoryActors = s.StoryActors
                        .Select(sa => new StoryDetailsStoryActorItemViewModel
                        {
                            Name = sa.Actor.Name,
                            Slug = sa.Actor.Slug,
                            HasImage = sa.Actor.Image != null
                        })
                        .ToList(),

                    StorySeries = s.StorySeries
                        .Select(ss => new StoryDetailsStorySeriesItemViewModel
                        {
                            Name = ss.Series.Name,
                            Slug = ss.Series.Slug
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if(vm is null)
            {
                return NotFound();
            }

            // Just ensure ordering is correct based on their chapter number (user defined chapter number)
            vm.Chapters = vm.Chapters
                .OrderBy(c => c.ChapterNumber)
                .ToList();

            return View(vm);
        }

        [HttpPost("/story/{id:guid}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var story = await _context.Stories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);
                
                if (story is null)
                {
                    return NotFound();
                }

                // prevents us reattaching the tracked instance
                _context.Stories.Remove(new Story {  Id = id });
                await _context.SaveChangesAsync();

                this.FlashSuccess($"Story {story.Title} deleted succesfully");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete story. StoryId={StoryId}", id);
                this.FlashError("We couldn't delete that story, please try again.");
                return RedirectToAction(nameof(View), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting story. StoryId={StoryId}", id);
                this.FlashError("An unexpected error occurred, please try again.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("/story/{id:guid}/export/{type}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(Guid id, string type, CancellationToken cancellationToken)
        {
            if (!_storyExportService.TryParseType(type, out var exportType))
            {
                return BadRequest("Invalid export type. Use 'html' or 'markdown'.");
            }

            try
            {
                var file = await _storyExportService.ExportStoryAsync(id, exportType, cancellationToken);
                return file;
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("/story/{id:guid}/nut")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNut(Guid id, [FromBody] NutReadDeltaRequest req, CancellationToken cancellationToken)
        {
            // TODO: This updates the UPDATED value of 'Story', need to change the way it saves to prevent that
            // For time being Created/Updated is hidden from Story.View

            if(req is null || (req.Delta != 1 && req.Delta != -1))
            {
                return BadRequest();
            }

            var story = await _context.Stories
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if(story is null)
            {
                return NotFound();
            }

            var newValue = story.NutCounter + req.Delta;
            story.NutCounter = newValue < 0 ? 0 : newValue;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { nutCounter = story.NutCounter });
        }

        [HttpPost("/story/{id:guid}/read")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRead(Guid id, [FromBody] NutReadDeltaRequest req, CancellationToken cancellationToken)
        {
            // TODO: This updates the UPDATED value of 'Story', need to change the way it saves to prevent that
            // For time being Created/Updated is hidden from Story.View

            if (req is null || (req.Delta != 1 && req.Delta != -1))
            {
                return BadRequest();
            }

            var story = await _context.Stories
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if(story is null)
            {
                return NotFound();
            }

            var newValue = story.ReadCounter + req.Delta;
            story.ReadCounter = newValue < 0 ? 0 : newValue;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { readCounter = story.ReadCounter });
        }
    }
}