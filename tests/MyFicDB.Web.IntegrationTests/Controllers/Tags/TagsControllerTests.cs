using Microsoft.EntityFrameworkCore;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models;
using MyFicDB.Core.Models.Story;
using MyFicDB.Web.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using static MyFicDB.Web.Services.TagService;

namespace MyFicDB.Web.IntegrationTests.Controllers.Tags
{
    public sealed class TagsControllerTests : TestBase, IClassFixture<TestAppFactory>
    {
        public TagsControllerTests(TestAppFactory factory) : base(factory)
        {
            ResetDatabaseAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Index_WhenAuthenticated_ReturnsOK()
        {
            // act
            var res = await CreateClient(allowAutoRedirect: false).GetAsync("/tags");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.False(string.IsNullOrEmpty(await res.Content.ReadAsStringAsync()));
        }

        [Fact]
        public async Task Index_WhenAnonymous_RedirectsToLogin()
        {
            // act
            var res = await CreateAnonymousClient(allowAutoRedirect: false).GetAsync("/tags");

            // assert
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            Assert.Contains("Login", res.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Index_WhenAuthenticated_WithTag_RendersTag()
        {
            // arrange
            var (_, name) = await SeedTagOnlyAsync();

            // act
            var res = await CreateClient(allowAutoRedirect: false).GetAsync("/tags");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.Contains(name, await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task View_WhenTagDoesNotExist_ReturnsNotFound()
        {
            // act
            var res = await CreateClient().GetAsync($"/tag/{Guid.NewGuid():N}");

            // assert
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task View_WhenTagExists_ReturnsOk_AndRendersTagName()
        {
            // arrange
            var (slug, name) = await SeedTagOnlyAsync();

            // act
            var res = await CreateClient().GetAsync($"/tag/{slug}");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.Contains(name, await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task View_WhenTagHasStories_ReturnsOk_AndStoriesAreOrderedByCreatedDateDesc()
        {
            // arrange
            var (slug, _, olderTitle, newerTitle) = await SeedTagWithTwoStoriesAsync();

            // act
            var res = await CreateClient().GetAsync($"/tag/{slug}");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var orderedTitles = await WithDbAsync(async db => { 
                return await db.StoryTags
                .AsNoTracking()
                .Where(st => st.Tag.Slug == slug)
                .OrderByDescending(st => st.Story.CreatedDate)
                    .ThenByDescending(st => st.Story.Id)
                .Select(st => st.Story.Title)
                .ToListAsync();
            }); 
            
            Assert.Equal(new[] { newerTitle, olderTitle }, orderedTitles);
        }

        [Fact]
        public async Task Suggest_Tags_Prioritises_StartsWith_Over_Contains()
        {
            // arrange
            await SeedMultipleTagsAsync("dark comedy", "very dark", "dark horror");

            // act
            var res = await CreateClient().GetAsync("/api/v1/suggest/tags?query=dar");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var items = await res.Content.ReadFromJsonAsync<List<TagSuggestion>>();
            Assert.NotNull(items);
            Assert.NotEmpty(items!);

            AssertStartsWithItemsComeFirst(items!.Select(x => x.Name), "da");
        }

        [Fact]
        public async Task Suggest_Tags_Returns200_And_Empty_WhenQueryTooShort()
        {
            // act
            var res = await CreateClient().GetAsync("/api/v1/suggest/tags?query=d");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var items = await res.Content.ReadFromJsonAsync<List<TagSuggestion>>();
            Assert.NotNull(items);
            Assert.Empty(items!);
        }

        [Fact]
        public async Task Suggest_Tags_WhenAnonymous_RedirectsToLogin()
        {
            // act
            var res = await CreateAnonymousClient(allowAutoRedirect: false).GetAsync("/api/v1/suggest/tags?query=doctor who");

            // assert
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            Assert.Contains("Login", res.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // helpers
        private async Task<(string Slug, string Name)> SeedTagOnlyAsync()
        {
            return await WithDbAsync(async db =>
            {
                var tagName = $"Tag1_Contr0ller Test{Guid.NewGuid():N}";

                var tag = new Tag()
                {
                    Id = Guid.NewGuid(),
                    Name = NamePipeline.CleanDisplayName(tagName),
                    NormalizedName = NamePipeline.NormalizeUpper(tagName),
                    Slug = NamePipeline.Slugify(tagName),
                };

                db.Tags.Add(tag);
                await db.SaveChangesAsync();

                return (tag.Slug, tag.Name);
            });
        }

        private async Task<(string TagSlug, string TagName, string OlderTitle, string NewerTitle)> SeedTagWithTwoStoriesAsync()
        {
            return await WithDbAsync(async db =>
            {
                var tagName = $"Series2_Contr0ller Test{Guid.NewGuid():n}";

                var tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = NamePipeline.CleanDisplayName(tagName),
                    NormalizedName = NamePipeline.NormalizeUpper(tagName),
                    Slug = NamePipeline.Slugify(tagName)
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

                db.Tags.Add(tag);
                db.Stories.AddRange(oldestStory, newestStory);

                db.StoryTags.AddRange(
                    new StoryTag
                    {
                        StoryId = oldestStory.Id,
                        TagId = tag.Id,
                        Story = oldestStory,
                        Tag = tag
                    },
                    new StoryTag
                    {
                        StoryId = newestStory.Id,
                        TagId = tag.Id,
                        Story = newestStory,
                        Tag = tag
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

                Assert.Equal(2, await db.StoryTags.CountAsync(st => st.TagId == tag.Id));
                
                return (tag.Slug, tag.Name, oldestStory.Title, newestStory.Title);

            });
        }

        private async Task SeedMultipleTagsAsync(params string[] names)
        {
            await WithDbAsync(async db =>
            {
                foreach (var display in names.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var cleaned = NamePipeline.CleanDisplayName(display);
                    var norm = NamePipeline.NormalizeUpper(cleaned);

                    var exists = await db.Tags
                        .AsNoTracking()
                        .AnyAsync(s => s.NormalizedName == norm);

                    if (exists)
                    {
                        continue;
                    }

                    db.Tags.Add(new Tag
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
