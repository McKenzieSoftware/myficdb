using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFicDB.Web.Services;

namespace MyFicDB.Web.Controllers.API
{
    [Authorize]
    [Route("api/v1/suggest")]
    public sealed class ApiSuggestionController : Controller
    {
        private readonly TagService _tagService;
        private readonly SeriesService _seriesService;
        private readonly ActorService _actorService;

        public ApiSuggestionController(TagService tagService, SeriesService seriesService, ActorService actorService)
        {
            _tagService = tagService;
            _seriesService = seriesService;
            _actorService = actorService;
        }

        [HttpGet("tags")]
        public async Task<IActionResult> GetTagSuggestions([FromQuery] string query, CancellationToken cancellationToken)
        {
            var results = await _tagService.SuggestAsync(query, 10, cancellationToken);
            return Ok(results);
        }

        [HttpGet("series")]
        public async Task<IActionResult> GetSeriesSuggestions([FromQuery] string query, CancellationToken cancellationToken)
        {
            var results = await _seriesService.SuggestAsync(query, 10, cancellationToken);
            return Ok(results);
        }

        [HttpGet("actors")]
        public async Task<IActionResult> GetActorSuggestions([FromQuery] string query, CancellationToken cancellationToken)
        {
            var results = await _actorService.SuggestAsync(query, 10, cancellationToken);
            return Ok(results);
        }
    }
}
