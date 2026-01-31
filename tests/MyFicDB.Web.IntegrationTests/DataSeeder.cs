using Microsoft.Extensions.DependencyInjection;
using MyFicDB.Core;
using MyFicDB.Core.Models.Story;

namespace MyFicDB.Web.IntegrationTests
{
    public static class DataSeeder
    {
        public static async Task<Guid> SeedStoryAsync(IServiceProvider services, string title = "Test Story")
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var story = new Story
            {
                Title = title,
                Summary = "Summary",
                Notes = "Notes",
                IsOwnWork = true,
                IsAIGenerated = false,
                IsNsfw = false
            };

            db.Stories.Add(story);
            await db.SaveChangesAsync();

            return story.Id;
        }
    }
}
