using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models;
using MyFicDB.Web.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;

namespace MyFicDB.Web.IntegrationTests.Controllers.Actors
{
    public sealed class ActorImageTests : TestBase, IClassFixture<TestAppFactory>
    {
        public ActorImageTests(TestAppFactory factory) : base(factory) { }

        [Fact]
        public async Task Actor_Create_WithImage_Allows_ImageEndpoiunt()
        {
            var client = CreateClient(allowAutoRedirect: false);

            var token = await AntiforgeryHelper.GetRequestVerificationTokenAsync(client, "/actor/create");

            // Tiny valid PNG (1x1)
            var pngBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7WZ1kAAAAASUVORK5CYII=");

            using var content = new MultipartFormDataContent
            {
                // Antiforgery mandtoery fields
                { new StringContent(token), "__RequestVerificationToken" },
                { new StringContent("William Hartnell"), "Name" },

                // these fields are not optional during /actor/create, only during /story/create
                { new StringContent("William Henry Hartnell (8 January 1908 – 23 April 1975) was an English actor, who is best known for portraying the first incarnation of the Doctor."), "Description" },
                { new StringContent("67"), "Age" }
            };

            // file
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            content.Add(fileContent, "Image", "test.png");

            var postRes = await client.PostAsync("/actor/create", content);

            // Expect redirect to /actor/{slug}
            Assert.Equal(HttpStatusCode.Redirect, postRes.StatusCode);
            Assert.NotNull(postRes.Headers.Location);

            // Extract slug from redirect location
            var location = postRes.Headers.Location!.ToString();

            // Expected /actor/{slug}
            var slug = location.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

            // Image endpoint should return png
            var imgRes = await client.GetAsync($"/actor/{slug}/image");
            Assert.Equal(HttpStatusCode.OK, imgRes.StatusCode);

            var ct = imgRes.Content.Headers.ContentType?.MediaType;
            Assert.Equal("image/png", ct);
        }

        // TODO: Need to change this to Returns404 for image when actor does not exist
        [Fact]
        public async Task Actor_ImageEndpoint_ReturnsOK_WhenNoImage()
        {
            var slug = await SeedActorWithoutImageAsync("No Image Actor");

            var client = CreateClient(allowAutoRedirect: false);
            var res = await client.GetAsync($"/actor/{slug}/image");

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        private Task<string> SeedActorWithoutImageAsync(string name)
        {
            return WithDbAsync(async db =>
            {
                var cleaned = NamePipeline.CleanDisplayName(name);
                var norm = NamePipeline.NormalizeUpper(cleaned);
                var slug = NamePipeline.Slugify(norm);

                if (!db.Actors.Any(a => a.NormalizedName == norm))
                {
                    db.Actors.Add(new Actor
                    {
                        Id = Guid.NewGuid(),
                        Name = cleaned,
                        NormalizedName = norm,
                        Slug = slug,
                        Description = null,
                        Age = null,
                        Image = null
                    });

                    await db.SaveChangesAsync();
                }

                return slug;
            });
        }
    }
}
