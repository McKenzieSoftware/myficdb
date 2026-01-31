using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Models.Story;
using MyFicDB.Web.ViewModels.GlobalSearch;
using MyFicDB.Web.ViewModels.Story;
using System.Linq.Expressions;

namespace MyFicDB.Web.Controllers
{
    /// <summary>
    /// Search Controller used across the app.  Currently only has Story search, which then searches for things like tags, actors etc. under a story
    /// TODO: Implement Actor, Tag, Series specific search. i.e search for "Donald Duck" and it comes up with just Donald Duck, not stories etc. that he's included in?
    /// NOTE: Need to decide if going to do /search or /entity/search.  Global Search is fine for now tho.
    /// </summary>

    [Authorize]
    [Route("search")]
    public sealed class GlobalSearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GlobalSearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string query)
        {
            if(string.IsNullOrWhiteSpace(query))
            {
                return View(new GlobalSearchIndexViewModel());
            }

            // Create fuzy pattern; Docter%Who instead of Docter Who
            string fuzzyPattern = $"%{query.Trim().ToLower().Replace(" ", "%")}%";

            // actual search query, search title, summary, storyactors, storytags and storyseries 
            // for fuzzyPattern

            Expression<Func<Story, bool>> searchQuery = s =>
                    EF.Functions.Like(s.Title, fuzzyPattern) ||
                    EF.Functions.Like(s.Summary, fuzzyPattern) ||
                    s.StoryActors.Any(sa => EF.Functions.Like(sa.Actor.Name, fuzzyPattern)) ||
                    s.StoryTags.Any(st => EF.Functions.Like(st.Tag.Name, fuzzyPattern)) ||
                    s.StorySeries.Any(ss => EF.Functions.Like(ss.Series.Name, fuzzyPattern));

            // Search for Stories
            var stories = await _context.Stories
                .AsNoTracking()
                .Where(searchQuery)
                .OrderBy(s => s.Title)
                .Take(10)
                .Select(s => new StoryCardViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Summary = s.Summary,
                    CreatedDate = s.CreatedDate,

                    Series = s.StorySeries
                        .Select(ss => new SeriesLinkRecord(ss.Series.Name, ss.Series.Slug))
                        .ToList(),

                    Tags = s.StoryTags
                        .Select(st => new TagLinkRecord(st.Tag.Name, st.Tag.Slug))
                        .ToList(),

                    TotalWordCount = s.Chapters
                        .Select(c => c.Content.WordCount)
                        .Sum(),
                    ChapterCount = s.Chapters.Count,
                    NutCount = s.NutCounter,
                    ReadCount = s.ReadCounter,

                    IsAIGenerated = s.IsAIGenerated,
                    IsOwnWork = s.IsOwnWork,
                    IsNsfw = s.IsNsfw
                })
                .ToListAsync();

            var vm = new GlobalSearchIndexViewModel
            {
                Stories = stories
            };

            return View(vm);
        }
    }
}
