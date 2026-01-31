using Microsoft.Extensions.DependencyInjection;
using MyFicDB.Core;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models;
using MyFicDB.Web.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using static MyFicDB.Web.Services.ActorService;

namespace MyFicDB.Web.IntegrationTests.ApiTests
{
    public sealed class SuggestionApiTests : TestBase, IClassFixture<TestAppFactory>
    {
        public SuggestionApiTests(TestAppFactory factory) : base(factory) { }

        [Fact]
        public async Task Suggest_Actors_Returns200_And_Empty_WhenQueryTooShort()
        {
            var client = CreateClient();

            var res = await client.GetAsync("/api/v1/suggest/actors?query=a");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var items = await res.Content.ReadFromJsonAsync<List<ActorSuggestion>>();
            Assert.NotNull(items);
            Assert.Empty(items!);
        }

        [Fact]
        public async Task Suggest_Actors_Prioritises_StartsWith_Over_Contains()
        {
            await SeedActorsAsync("Donald Duck", "The Donald de Doo", "Donny The Dealer", "Sir Donny da Dealer");

            var client = CreateClient();
            var res = await client.GetAsync("/api/v1/suggest/actors?query=don");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var items = await res.Content.ReadFromJsonAsync<List<ActorSuggestion>>();
            Assert.NotNull(items);
            Assert.NotEmpty(items!);

            AssertStartsWithItemsComeFirst(items!.Select(x => x.Name), "don");
        }

        private async Task SeedActorsAsync(params string[] names)
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var display in names)
            {
                var cleaned = NamePipeline.CleanDisplayName(display);
                var norm = NamePipeline.NormalizeUpper(cleaned);
                var slug = NamePipeline.Slugify(norm);

                if (db.Actors.Any(a => a.NormalizedName == norm))
                {
                    continue;
                }

                db.Actors.Add(new Actor
                {
                    Id = Guid.NewGuid(),
                    Name = cleaned,
                    NormalizedName = norm,
                    Slug = slug,
                    Description = null,
                    Age = null
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
