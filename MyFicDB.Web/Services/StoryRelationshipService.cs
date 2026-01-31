using MyFicDB.Core;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models.Story;

namespace MyFicDB.Web.Services
{
    /// <summary>
    /// Deals with Story to/from Tag/Series/Actor/xx relationship parsing and join updates
    /// </summary>
    public sealed class StoryRelationshipService
    {
        private readonly ApplicationDbContext _context;
        private readonly TagService _tagService;
        private readonly SeriesService _seriesService;
        private readonly ActorService _actorService;

        public StoryRelationshipService(ApplicationDbContext context, TagService tagService, SeriesService seriesService, ActorService actorService)
        {
            _context = context;
            _tagService = tagService;
            _seriesService = seriesService;
            _actorService = actorService;
        }

        public async Task ApplyAsync(Story story, string? tagsCsv, string? seriesCsv, string? actorsCsv, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(story);

            var tagNames = NamePipeline.ParseCsvAndClean(tagsCsv, max: 30);
            var seriesNames = NamePipeline.ParseCsvAndClean(seriesCsv, max: 30);
            var actorNames = NamePipeline.ParseCsvAndClean(actorsCsv, max: 30);

            // resolve / create entities
            var tags = await _tagService.GetOrCreateAsync(tagNames, cancellationToken);
            var series = await _seriesService.GetOrCreateAsync(seriesNames, cancellationToken);
            var actors = await _actorService.GetOrCreateAsync(actorNames, cancellationToken);

            // joins
            await ApplyTagsAsync(story, tags.Select(t => t.Id).ToHashSet(), cancellationToken);
            await ApplySeriesAsync(story, series.Select(s => s.Id).ToHashSet(), cancellationToken);
            await ApplyActorsAsync(story, actors.Select(a => a.Id).ToHashSet(), cancellationToken);
        }

        private Task ApplyTagsAsync(Story story, HashSet<Guid> desiredTagIds, CancellationToken ct)
        {
            // Current joins
            var currentJoins = story.StoryTags.ToList();
            var currentIds = currentJoins.Select(j => j.TagId).ToHashSet();

            // Remove not desired
            var toRemove = currentJoins.Where(j => !desiredTagIds.Contains(j.TagId)).ToList();
            if (toRemove.Count > 0)
            {
                _context.StoryTags.RemoveRange(toRemove);
            }

            // Add missing
            foreach (var tagId in desiredTagIds)
            {
                if (currentIds.Contains(tagId))
                {
                    continue;
                }

                story.StoryTags.Add(new StoryTag
                {
                    StoryId = story.Id,
                    TagId = tagId
                });
            }

            return Task.CompletedTask;
        }

        private Task ApplySeriesAsync(Story story, HashSet<Guid> desiredSeriesIds, CancellationToken ct)
        {
            var currentJoins = story.StorySeries.ToList();
            var currentIds = currentJoins.Select(j => j.SeriesId).ToHashSet();

            var toRemove = currentJoins.Where(j => !desiredSeriesIds.Contains(j.SeriesId)).ToList();
            if (toRemove.Count > 0)
            {
                _context.StorySeries.RemoveRange(toRemove);
            }

            foreach (var seriesId in desiredSeriesIds)
            {
                if (currentIds.Contains(seriesId))
                {
                    continue;
                }

                story.StorySeries.Add(new StorySeries
                {
                    StoryId = story.Id,
                    SeriesId = seriesId
                });
            }

            return Task.CompletedTask;
        }

        private Task ApplyActorsAsync(Story story, HashSet<Guid> desiredActorIds, CancellationToken ct)
        {
            var currentJoins = story.StoryActors.ToList();
            var currentIds = currentJoins.Select(j => j.ActorId).ToHashSet();

            var toRemove = currentJoins.Where(j => !desiredActorIds.Contains(j.ActorId)).ToList();
            if (toRemove.Count > 0)
            {
                _context.StoryActors.RemoveRange(toRemove);
            }

            foreach (var actorId in desiredActorIds)
            {
                if (currentIds.Contains(actorId))
                {
                    continue;
                }

                story.StoryActors.Add(new StoryActor
                {
                    StoryId = story.Id,
                    ActorId = actorId
                });
            }

            return Task.CompletedTask;
        }
    }
}