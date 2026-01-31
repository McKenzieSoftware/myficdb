using Microsoft.EntityFrameworkCore;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models.Story;
using MyFicDB.Web.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using static MyFicDB.Web.Services.SeriesService;

namespace MyFicDB.Web.IntegrationTests.Controllers.Series
{
    public sealed class SeriesControllerTests : TestBase, IClassFixture<TestAppFactory>
    {
        public SeriesControllerTests(TestAppFactory factory) : base(factory)
        {
            ResetDatabaseAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Index_WhenAuthenticated_ReturnsOK()
        {
            // act
            var res = await CreateClient(allowAutoRedirect: false).GetAsync("/series");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.False(string.IsNullOrEmpty(await res.Content.ReadAsStringAsync()));
        }

        [Fact]
        public async Task Index_WhenAnonymous_RedirectsToLogin()
        {
            // act
            var res = await CreateAnonymousClient(allowAutoRedirect: false).GetAsync("/series");

            // assert
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            Assert.Contains("Login", res.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Index_WhenAuthenticated_WithSeries_RendersSeries()
        {
            // arrange
            var (slug, name) = await SeedSeriesOnlyAsync();

            // act
            var res = await CreateClient(allowAutoRedirect: false).GetAsync("/series");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.Contains(name, await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task View_WhenSeriesDoesNotExist_ReturnsNotFound()
        {
            // act
            var res = await CreateClient().GetAsync($"/series/{Guid.NewGuid():N}");

            // assert
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task View_WhenSeriesSlugIsWhitespace_ReturnsNotFound()
        {
            // act -- %20 is whitespace, or " "
            var res = await CreateClient().GetAsync($"/series/%20");

            // asert
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task View_WhenSeriesExists_ReturnsOk_AndRendersSeriesName()
        {
            // arrange
            var (slug, name) = await SeedSeriesOnlyAsync();

            // act
            var res = await CreateClient().GetAsync($"/series/{slug}");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.Contains(name, await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task View_WhenSeriesHasStories_ReturnsOk_AndStoriesAreOrderedByCreatedDateDesc()
        {
            // arrange
            var (slug, _seriesName, olderTitle, newerTitle) = await SeedSeriesWithTwoStoriesAsync();

            // act
            var res = await CreateClient().GetAsync($"/series/{slug}");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var orderedTitles = await WithDbAsync(async db => {
                return await db.StorySeries
                .AsNoTracking()
                .Where(ss => ss.Series.Slug == slug)
                .OrderByDescending(st => st.Story.CreatedDate)
                    .ThenByDescending(st => st.Story.Id)
                .Select(st => st.Story.Title)
                .ToListAsync();
            });

            Assert.Equal(new[] { newerTitle, olderTitle }, orderedTitles);
        }

        [Fact]
        public async Task Suggest_Series_Prioritises_StartsWith_Over_Contains()
        {
            // arrange
            await SeedMultipleSeriesAsync("Doctor Who Classic", "Docter Who 2005", "Docter Who 2025");

            // act
            var res = await CreateClient().GetAsync("/api/v1/suggest/series?query=doc");
   
            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var items = await res.Content.ReadFromJsonAsync<List<SeriesSuggestion>>();

            Assert.NotNull(items);
            Assert.NotEmpty(items!);

            AssertStartsWithItemsComeFirst(items!.Select(x => x.Name), "doc");
        }

        [Fact]
        public async Task Suggest_Series_Returns200_And_Empty_WhenQueryTooShort()
        {
            // act
            var res = await CreateClient().GetAsync("/api/v1/suggest/series?query=d");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var items = await res.Content.ReadFromJsonAsync<List<SeriesSuggestion>>();

            Assert.NotNull(items);
            Assert.Empty(items!);
        }

        // helpers
        private async Task<(string Slug, string Name)> SeedSeriesOnlyAsync()
        {
            return await WithDbAsync(async db =>
            {
                var seriesName = $"Series1_Contr0ller Test{Guid.NewGuid():n}";

                var series = new Core.Models.Series()
                {
                    Id = Guid.NewGuid(),
                    Name = NamePipeline.CleanDisplayName(seriesName),
                    NormalizedName = NamePipeline.NormalizeUpper(seriesName),
                    Slug = NamePipeline.Slugify(seriesName)
                };

                db.Series.Add(series);
                await db.SaveChangesAsync();

                return (series.Slug, series.Name);
            });
        }

        private async Task<(string SeriesSlug, string SeriesName, string OlderTitle, string NewerTitle)> SeedSeriesWithTwoStoriesAsync()
        {
            return await WithDbAsync(async db =>
            {
                var seriesName = $"Series2_Contr0ller Test{Guid.NewGuid():n}";

                var series = new Core.Models.Series
                {
                    Id = Guid.NewGuid(),
                    Name = NamePipeline.CleanDisplayName(seriesName),
                    NormalizedName = NamePipeline.NormalizeUpper(seriesName),
                    Slug = NamePipeline.Slugify(seriesName),
                };

                var oldestStory = new Story
                {
                    Id = Guid.NewGuid(),
                    Title = "Oldest Story",
                    Summary = "oldest story summary",
                    Notes = string.Empty
                };

                var newestStory = new Story
                {
                    Id = Guid.NewGuid(),
                    Title = "Newest Story",
                    Summary = "newest story summary",
                    Notes = string.Empty
                };

                db.Series.Add(series);
                db.Stories.AddRange(oldestStory, newestStory);

                db.StorySeries.AddRange(
                    new StorySeries
                    {
                        StoryId = oldestStory.Id,
                        SeriesId = series.Id,
                        Story = oldestStory,
                        Series = series
                    },
                    new StorySeries
                    {
                        StoryId = newestStory.Id,
                        SeriesId = series.Id,
                        Story = newestStory,
                        Series = series
                    });

                await db.SaveChangesAsync();

                // all models use a base model that means CreateDate and UpdatedDate are created automatically and modified on save
                // so i can't set the created date above because it gets overwritten by the SaveChangesAsync() helper in MyFicDB.Core.ApplicationDbContext.cs
                // need to do this 'hack' for this test to pass, this is just a workaround and if i didnt use SaveChangesAsync() it wouldn't be an issue
                // but fuck assigning Created/Update date manually for ever model.
                // this is literally the only way i could get this to work consistently because of fight backs from SaveChangesAsync(); test is still valid though
                var now = DateTimeOffset.UtcNow;

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE tblStories SET CreatedDate = {now.AddDays(-10)} WHERE Id = {oldestStory.Id};");

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE tblStories SET CreatedDate = {now.AddDays(-1)} WHERE Id = {newestStory.Id};");

                Assert.Equal(2, await db.StorySeries.CountAsync(ss => ss.SeriesId == series.Id));

                return (series.Slug, series.Name, oldestStory.Title, newestStory.Title);

            });
        }
    
        private async Task SeedMultipleSeriesAsync(params string[] names)
        {
            await WithDbAsync(async db =>
            {
                foreach (var display in names.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var cleaned = NamePipeline.CleanDisplayName(display);
                    var norm = NamePipeline.NormalizeUpper(cleaned);

                    var exists = await db.Series
                        .AsNoTracking()
                        .AnyAsync(s => s.NormalizedName == norm);

                    if (exists)
                    {
                        continue;
                    }

                    db.Series.Add(new Core.Models.Series
                    {
                        Id = Guid.NewGuid(),
                        Name = cleaned,
                        NormalizedName = norm,
                        Slug = NamePipeline.Slugify(norm)
                    });
                }

                await db.SaveChangesAsync();
            });
        }
    }
}
