using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFicDB.Core;
using MyFicDB.Web.IntegrationTests.Infrastructure;
using System.Net;

namespace MyFicDB.Web.IntegrationTests.Controllers.Stories
{
    public sealed class StoryEditRegressionTests : TestBase, IClassFixture<TestAppFactory>
    {
        public StoryEditRegressionTests(TestAppFactory factory) : base(factory) { }

        [Fact]
        public async Task Story_Edit_Updates_Tags_Series_Actors_JoinsCorrectly()
        {
            var client = CreateClient(allowAutoRedirect: false);

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

                ["Tags"] = "adventure, science fiction, time travel",
                ["Series"] = "Doctor Who Classic",
                ["Actors"] = "William Hartnell, Carole Ann Ford"
            };

            var createRes = await client.PostAsync("/story/create", new FormUrlEncodedContent(createForm));
            Assert.Equal(HttpStatusCode.Redirect, createRes.StatusCode);

            var storyId = UrlIdHelper.ExtractGuidFromLocation(createRes.Headers.Location);

            var editToken = await AntiforgeryHelper.GetRequestVerificationTokenAsync(client, $"/story/{storyId}/edit");

            // Edit: remove adventure, add classic, remove series, add Jacqueline Hill, remove Carole Ann Ford
            var editForm = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Id"] = storyId.ToString(),
                ["Title"] = "An Unearthly Child (Edited)",
                ["Summary"] = "<p>Hello again</p>",
                ["Notes"] = "Notes 2",
                ["IsOwnWork"] = "true",
                ["IsAIGenerated"] = "false",
                ["IsNsfw"] = "false",

                ["Tags"] = "science fiction, time travel, classic",
                ["Series"] = "",
                ["Actors"] = "William Hartnell, Jacqueline Hill"
            };

            var editRes = await client.PostAsync($"/story/{storyId}/edit", new FormUrlEncodedContent(editForm));
            Assert.Equal(HttpStatusCode.Redirect, editRes.StatusCode);

            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Should only return 3 results
            var tagNames = await db.StoryTags
                .Where(st => st.StoryId == storyId)
                .Select(st => st.Tag.Name)
                .ToListAsync();

            Assert.Equal(3, tagNames.Count);
            Assert.Contains(tagNames, t => t.Equals("science fiction", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(tagNames, t => t.Equals("time travel", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(tagNames, t => t.Equals("classic", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(tagNames, t => t.Equals("adventure", StringComparison.OrdinalIgnoreCase));

            // Series should be empty
            var seriesCount = await db.StorySeries.CountAsync(ss => ss.StoryId == storyId);
            Assert.Equal(0, seriesCount);

            // Actors should be William and Jacqueline only
            var actorNames = await db.StoryActors
                .Where(sa => sa.StoryId == storyId)
                .Select(sa => sa.Actor.Name)
                .ToListAsync();

            Assert.Equal(2, actorNames.Count);
            Assert.Contains(actorNames, a => a.Equals("William Hartnell", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(actorNames, a => a.Equals("Jacqueline Hill", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(actorNames, a => a.Equals("Carole Ann Ford", StringComparison.OrdinalIgnoreCase));
        }
    }
}
