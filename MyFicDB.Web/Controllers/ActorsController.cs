using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MyFicDB.Core;
using MyFicDB.Core.Extensions;
using MyFicDB.Core.Models;
using MyFicDB.Web.Services;
using MyFicDB.Web.ViewModels.Actor;
using MyFicDB.Web.ViewModels.Story;
using System.Text;

namespace MyFicDB.Web.Controllers
{
    [Authorize]
    [Route("actors")]
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ActorService _actorService;
        private readonly ILogger<ActorsController> _logger;

        public ActorsController(ApplicationDbContext context, ActorService actorService, ILogger<ActorsController> logger)
        {
            _context = context;
            _actorService = actorService;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("{page:int}")]
        public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
        {
            const int pageSize = 20;
            const int pageWindowSize = 5;

            if (page < 1)
            {
                page = 1;
            }

            var totalActors = await _context.Actors.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalActors / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var actors = await _context.Actors
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedDate)
                    .ThenBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActorIndexItemViewModel
                {
                    Name = a.Name,
                    Slug = a.Slug,
                    HasImage = a.Image != null
                })
                .ToListAsync(cancellationToken);

            var vm = new ActorIndexPagedViewModel
            {
                Actors = actors,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                PageWindowSize = pageWindowSize
            };

            return View(vm);
        }

        [HttpGet("/actor/create")]
        public IActionResult Create()
        {
            return View(new ActorCreateViewModel());
        }

        [HttpPost("/actor/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActorCreateViewModel vm, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                var actor = await _actorService.CreateAsync(
                    name: vm.Name,
                    description: vm.Description ?? string.Empty,
                    age: vm.Age,
                    image: vm.Image,
                    cancellationToken: cancellationToken);

                this.FlashSuccess("Actor created.");
                return RedirectToAction("View", new { slug = actor.Slug });
            }
            catch (InvalidOperationException ex)
            {
                // "expected" failures from service (image, etc)
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpGet("/actor/{slug}/image")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]
        public async Task<IActionResult> Image(string slug, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var actor = await _context.Actors
                .AsNoTracking()
                .Include(a => a.Image)
                .Where(a => a.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (actor == null)
            {
                return NotFound();
            }

            var img = actor.Image == null ? null : new
            {
                actor.Image.Data,
                actor.Image.ContentType,
                actor.Image.Sha256
            };

            if (img is null || img.Data is null || img.Data.Length == 0 || string.IsNullOrWhiteSpace(img.ContentType))
            {
                return NoImageSvgResult();
            }

            // ETag support (prefer stored hash, otherwise compute a cheap fallback)
            var etag = !string.IsNullOrWhiteSpace(img.Sha256)
                ? $"\"{img.Sha256}\""
                : $"\"{img.Data.Length}\"";

            if (Request.Headers.IfNoneMatch == etag)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = etag;

            return File(img.Data, img.ContentType);
        }

        [HttpGet("/actor/{slug}")]
        public async Task<IActionResult> View(string slug, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var vm = await _context.Actors
                .AsNoTracking()
                .Where(a => a.Slug == slug)
                .Select(a => new ActorViewViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Slug = a.Slug,
                    Description = a.Description,
                    Age = a.Age,
                    HasImage = a.Image != null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (vm is null)
            {
                return NotFound();
            }

            vm.Stories = await _context.StoryActors
                .AsNoTracking()
                .Where(sa => sa.ActorId == vm.Id)
                .Select(sa => sa.Story)
                .Distinct()
                .Select(s => new ActorStoryListItemViewModel
                {
                    StoryId = s.Id,
                    Title = s.Title,
                    Summary = s.Summary,
                    ChapterCount = s.Chapters.Count
                })
                .OrderByDescending(x => x.ChapterCount)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken);

            return View(vm);
        }

        [HttpGet("/actor/{slug}/edit")]
        public async Task<IActionResult> Edit(string slug, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var vm = await _context.Actors
                .AsNoTracking()
                .Where(a => a.Slug == slug)
                .Select(a => new ActorEditViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Slug = a.Slug,
                    Description = a.Description,
                    Age = a.Age,
                    HasImage = a.Image != null
                })
                .FirstOrDefaultAsync(cancellationToken);

            return vm is null ? NotFound() : View(vm);
        }

        [HttpPost("/actor/{slug}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string slug, ActorEditViewModel vm, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                var updated = await _actorService.UpdateAsync(
                    actorId: vm.Id,
                    routeSlug: slug,
                    name: vm.Name,
                    description: vm.Description,
                    age: vm.Age,
                    image: vm.Image,
                    removeImage: vm.RemoveImage,
                    cancellationToken: cancellationToken);

                this.FlashSuccess("Actor updated");
                return RedirectToAction("View", new { slug = updated.Slug });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                // Preserve current HasImage state so the view renders correctly
                vm.HasImage = await _context.Actors.AsNoTracking()
                    .AnyAsync(a => a.Id == vm.Id && a.Image != null, cancellationToken);

                return View(vm);
            }
        }

        [HttpPost("/actor/{slug}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
        {
            var actor = await _context.Actors
                .AsNoTracking()
                .Where(a => a.Slug == slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (actor is null)
            {
                return NotFound();
            }

            try
            {
                _context.Actors.Remove(actor);
                await _context.SaveChangesAsync(cancellationToken);

                this.FlashSuccess($"Actor {actor.Name} deleted succesfully");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete Actor. Actor={slug}", slug);
                this.FlashError($"We couldn't delete the Actor {actor.Name} due to it being assigned to a Story, please remove the relationship and try again.");
                return RedirectToAction(nameof(View), new { slug = actor.Slug });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting Actor. Actor={slug}", slug);
                this.FlashError("An unexpected error occurred, please try again.");
                return RedirectToAction(nameof(Index));
            }
        }

        // helpers
        private IActionResult NoImageSvgResult()
        {
            // Stable ETag for the placeholder
            const string etag = "\"no-image-v1\"";
            if (Request.Headers.IfNoneMatch == etag)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = etag;

            // Helps avoid MIME sniffing issues
            Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

            // 200x200 placeholder SVG
            const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200" role="img" aria-label="No Image">
              <rect width="200" height="200" fill="#3b3b3b"/>
              <text x="100" y="108" text-anchor="middle" font-family="system-ui, -apple-system, Segoe UI, Roboto, Arial" font-size="20" fill="#FFFFFF">
                No Image
              </text>
            </svg>
            """;

            return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml; charset=utf-8");
        }
    }
}
