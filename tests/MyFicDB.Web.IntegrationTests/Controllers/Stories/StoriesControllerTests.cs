using Microsoft.EntityFrameworkCore;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models;
using MyFicDB.Core.Models.Story;
using MyFicDB.Web.IntegrationTests.Infrastructure;
using NuGet.ContentModel;
using System.Net;

namespace MyFicDB.Web.IntegrationTests.Controllers.Stories
{
    public sealed class StoriesControllerTests : TestBase, IClassFixture<TestAppFactory>
    {
        public StoriesControllerTests(TestAppFactory factory) : base(factory)
        {
            ResetDatabaseAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Index_WhenAuthenticated_ReturnsOK()
        {
            // act
            var res = await CreateClient(allowAutoRedirect: false).GetAsync("/stories");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.False(string.IsNullOrEmpty(await res.Content.ReadAsStringAsync()));
        }

        [Fact]
        public async Task Index_WhenAnonymous_RedirectsToLogin()
        {
            // act
            var res = await CreateAnonymousClient(allowAutoRedirect: false).GetAsync("/stories");

            // assert
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            Assert.Contains("Login", res.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        }

        [Fact]
        public async Task Index_WhenAuthenticated_WithStory_RendersStory()
        {
            // arrange
            var (_, name, _) = await SeedStoryOnlyAsync();

            // act
            var res = await CreateClient(allowAutoRedirect: false).GetAsync("/stories");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.Contains(name, await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task View_WhenStoryDoesNotExist_ReturnsNotFound()
        {
            // act
            var res = await CreateClient().GetAsync($"/story/{Guid.NewGuid():N}");

            // assert
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task View_WhenStoryDoesExist_ReturnsOk_AndRendersStoryName()
        {
            // arrange
            var (id, name, summary) = await SeedStoryOnlyAsync();

            // act
            var res = await CreateClient().GetAsync($"/story/{id}");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            Assert.Contains(name, await res.Content.ReadAsStringAsync());
            Assert.Contains(summary, await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task View_WhenStoryHasEntities_ReturnsOk()
        {
            // arrange
            var (oldestStory, newestStory) = await SeedStoriesMultipleEntities();

            // act
            var res = await CreateClient().GetAsync($"/story/{newestStory.Id}");

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var html = await res.Content.ReadAsStringAsync();

            Assert.Contains(newestStory.Title, html);
            Assert.Contains(newestStory.Summary!, html);
            Assert.Contains(newestStory.StoryTags.Select(st => st.Tag.Name).First(), html);
            //Assert.Contains(newestStory.StorySeries.Select(ss => ss.Series.Name).First(), html); // Note: Not currently included in Stories.View
            Assert.Contains(newestStory.StoryActors.Select(sa => sa.Actor.Name).First(), html);
        }

        [Fact]
        public async Task Create_Story_Redirect_OnSuccess()
        {
            // act
            var client = CreateClient(allowAutoRedirect: false);

            var token = await AntiforgeryHelper.GetRequestVerificationTokenAsync(client, "/story/create");

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,

                ["Title"] = "An Unearthly Child",
                ["Summary"] = "<p>Hello</p>",
                ["Notes"] = "Notes",
                ["IsOwnWork"] = "true",
                ["IsAIGenerated"] = "false",
                ["IsNsfw"] = "false",
                ["Tags"] = "adventure, science fiction, time travel",
                ["Series"] = "Doctor Who",
                ["Actors"] = "William Hartnell, Jacqueline Hill"
            };

            var res = await client.PostAsync("/story/create", new FormUrlEncodedContent(form));

            // asert
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
        }

        [Fact]
        public async Task Create_Story_WithMissingTitle_Returns200_AndShowsValidation()
        {
            // act
            var client = CreateClient(allowAutoRedirect: false);

            var token = await AntiforgeryHelper.GetRequestVerificationTokenAsync(client, "/story/create");

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Title"] = "", // invalid
                ["Summary"] = "<p>Hello</p>",
                ["Notes"] = "Notes",
                ["IsOwnWork"] = "true",
                ["IsAIGenerated"] = "false",
                ["IsNsfw"] = "false",
                ["Tags"] = "",
                ["Series"] = "",
                ["Actors"] = ""
            };

            var res = await client.PostAsync("/story/create", new FormUrlEncodedContent(form));

            // assert
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var html = await res.Content.ReadAsStringAsync();

            // check for validation error
            Assert.Contains("Title", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Delete_Story_Redirect_OnSuccess()
        {
            // act
            var client = CreateClient(allowAutoRedirect: false);

            // >> create
            var createToken = await AntiforgeryHelper.GetRequestVerificationTokenAsync(client, "/story/create");

            var createForm = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Title"] = "An Unearthly Child",
                ["Summary"] = "<p>Hello</p>",
                ["Notes"] = "Notes",
                ["IsOwnWork"] = "true",
                ["IsAIGenerated"] = "false",
                ["IsNsfw"] = "false",
            };

            var createRes = await client.PostAsync("/story/create", new FormUrlEncodedContent(createForm));
            Assert.Equal(HttpStatusCode.Redirect, createRes.StatusCode);

            var storyId = UrlIdHelper.ExtractGuidFromLocation(createRes.Headers.Location);

            // >> delete
            var deleteToken = await AntiforgeryHelper.GetTokenAsync(client);

            var deleteForm = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = deleteToken
            };

            var deleteRes = await client.PostAsync($"/story/{storyId}/delete", new FormUrlEncodedContent(deleteForm));

            Assert.Equal(HttpStatusCode.Redirect, deleteRes.StatusCode);
            Assert.Equal("/stories", deleteRes.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Delete_Story_Returns404_NotFound_When_Missing()
        {
            var client = CreateClient();

            var token = await AntiforgeryHelper.GetTokenAsync(client);

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            };

            var res = await client.PostAsync($"/story/{Guid.NewGuid()}/delete", new FormUrlEncodedContent(form));

            Asset.Equals(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Delete_Story_Requires_Antiforgery()
        {
            var client = CreateClient();

            var res = await client.PostAsync($"/story/{Guid.NewGuid()}/delete", new FormUrlEncodedContent([]));

            Asset.Equals(HttpStatusCode.BadRequest, res.StatusCode);
        }
        // helpers

        private async Task<(Guid Id, string Name, string Summary)> SeedStoryOnlyAsync()
        {
            return await WithDbAsync(async db =>
            {
                var storyName = $"StoryName-{Guid.NewGuid:N}";

                var story = new Story()
                {
                    Id = Guid.NewGuid(),
                    Title = storyName,
                    Summary = "This is an example summary for the SeedOnlyAsync Helper"
                };

                db.Stories.Add(story);
                await db.SaveChangesAsync();

                return (story.Id, story.Title, story.Summary);

            });
        }
    
        private async Task<(Story oldestStory, Story newestStory)> SeedStoriesMultipleEntities()
        {
            return await WithDbAsync(async db =>
            {
                static Tag NewTag(string prefix)
                {
                    var token = Guid.NewGuid().ToString("N");
                    var display = NamePipeline.CleanDisplayName($"{prefix}{token}");

                    return new Tag
                    {
                        Id = Guid.NewGuid(),
                        Name = display,
                        NormalizedName = NamePipeline.NormalizeUpper(display),
                        Slug = NamePipeline.Slugify(display)
                    };
                }

                static Core.Models.Series NewSeries(string prefix)
                {
                    var token = Guid.NewGuid().ToString("N");
                    var display = NamePipeline.CleanDisplayName($"{prefix}{token}");

                    return new Core.Models.Series
                    {
                        Id = Guid.NewGuid(),
                        Name = display,
                        NormalizedName = NamePipeline.NormalizeUpper(display),
                        Slug = NamePipeline.Slugify(display)
                    };
                }

                static Actor NewActor(string prefix)
                {
                    var token = Guid.NewGuid().ToString("N");
                    var display = NamePipeline.CleanDisplayName($"{prefix}{token}");

                    return new Actor
                    {
                        Id = Guid.NewGuid(),
                        Name = display,
                        NormalizedName = NamePipeline.NormalizeUpper(display),
                        Slug = NamePipeline.Slugify(display),
                    };
                }

                var tag0 = NewTag("StoriesControllerTest_Tag0_");
                var tag1 = NewTag("StoriesControllerTest_Tag1_");

                var series0 = NewSeries("StoriesControllerTest_Series0_");
                var series1 = NewSeries("StoriesControllerTest_Series1_");

                var actor0 = NewActor("StoriesControllerTest_Actor0_");

                db.Tags.AddRange(tag0, tag1);
                db.Series.AddRange(series0, series1);
                db.Actors.Add(actor0);

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

                db.Stories.AddRange(oldestStory, newestStory);

                db.StoryTags.AddRange(
                    new StoryTag { StoryId = oldestStory.Id, TagId = tag0.Id },
                    new StoryTag { StoryId = oldestStory.Id, TagId = tag1.Id },
                    new StoryTag { StoryId = newestStory.Id, TagId = tag0.Id },
                    new StoryTag { StoryId = newestStory.Id, TagId = tag1.Id }
                );

                db.StorySeries.AddRange(
                    new StorySeries { StoryId = oldestStory.Id, SeriesId = series0.Id },
                    new StorySeries { StoryId = newestStory.Id, SeriesId = series1.Id }
                );

                db.StoryActors.AddRange(
                    new StoryActor { StoryId = oldestStory.Id, ActorId = actor0.Id },
                    new StoryActor { StoryId = newestStory.Id, ActorId = actor0.Id }
                );

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

                // RELOAD with includes so the returned objects match what the page should render
                var hydratedOldest = await db.Stories
                    .AsNoTracking()
                    .Include(s => s.StoryTags).ThenInclude(st => st.Tag)
                    .Include(s => s.StorySeries).ThenInclude(ss => ss.Series)
                    .Include(s => s.StoryActors).ThenInclude(sa => sa.Actor)
                    .SingleAsync(s => s.Id == oldestStory.Id);

                var hydratedNewest = await db.Stories
                    .AsNoTracking()
                    .Include(s => s.StoryTags).ThenInclude(st => st.Tag)
                    .Include(s => s.StorySeries).ThenInclude(ss => ss.Series)
                    .Include(s => s.StoryActors).ThenInclude(sa => sa.Actor)
                    .SingleAsync(s => s.Id == newestStory.Id);

                return (hydratedOldest, hydratedNewest);
            });
        }
    
    }
}
