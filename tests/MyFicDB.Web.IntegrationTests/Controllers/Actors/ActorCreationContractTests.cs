using Microsoft.Extensions.DependencyInjection;
using MyFicDB.Core;
using MyFicDB.Web.IntegrationTests.Infrastructure;
using System.Net;

namespace MyFicDB.Web.IntegrationTests.Controllers.Actors
{
    public sealed class ActorCreationContractTests : TestBase, IClassFixture<TestAppFactory>
    {

        public ActorCreationContractTests(TestAppFactory factory) : base(factory) { }   

        [Fact]
        public async Task Story_Create_Can_Create_Partial_Actor_ByNameOnly()
        {
            var client = CreateClient(allowAutoRedirect: false);

            var token = await AntiforgeryHelper.GetRequestVerificationTokenAsync(client, "/story/create");

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Title"] = "The Daleks",
                ["Summary"] = "<p>Hello</p>",
                ["Notes"] = "Notes",
                ["IsOwnWork"] = "true",
                ["IsAIGenerated"] = "false",
                ["IsNsfw"] = "false",
                ["Tags"] = "Adventure",
                ["Series"] = "Doctor Who",
                ["Actors"] = "William Russell"
            };

            var res = await client.PostAsync("/story/create", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

            // Verify actor exists and is partial
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var actor = db.Actors.SingleOrDefault(a => a.Name == "William Russell");
            Assert.NotNull(actor);
            Assert.True(string.IsNullOrWhiteSpace(actor!.Description));
            Assert.Null(actor.Age);
        }
    }
}
