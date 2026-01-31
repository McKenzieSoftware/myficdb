using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MyFicDB.Core;
using System.Threading.Tasks.Dataflow;

namespace MyFicDB.Web.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Base class for integration tests using TestAppFactory. Ensures the host is started before accessing Services/DbContext.
    /// </summary>
    public abstract class TestBase
    {
        protected TestAppFactory Factory { get; }

        private bool _hostStarted;
        private IServiceProvider? _services;

        protected TestBase(TestAppFactory factory)
        {
            Factory = factory;
        }

        /// <summary>
        /// Host-backed services. Safe to use after EnsureHostStarted().
        /// </summary>
        protected IServiceProvider Services
        {
            get
            {
                EnsureHostStarted();
                return _services!;
            }
        }

        /// <summary>
        /// Forces WebApplicationFactory to build and start the host
        /// </summary>
        protected void EnsureHostStarted()
        {
            if (_hostStarted)
            {
                return;
            }

            // Forces CreateHost() to run
            _ = Factory.CreateClient();

            _services = Factory.Services;
            _hostStarted = true;
        }

        /// <summary>
        /// Creates a HttpClient and ensures the host is started
        /// </summary>
        protected HttpClient CreateClient(bool allowAutoRedirect = false)
        {
            EnsureHostStarted();
            return Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect
            });
        }

        /// <summary>
        /// Creates a HttpClient without Authorization
        /// </summary>
        protected HttpClient CreateAnonymousClient(bool allowAutoRedirect = false)
        {
            var anonFactory = new TestAppFactory(enableAuth: false);

            return anonFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect
            });
        }

        /// <summary>
        /// Creates a scope from the host-backed service provider
        /// </summary>
        protected IServiceScope CreateScope()
        {
            EnsureHostStarted();
            return Services.CreateScope();
        }

        /// <summary>
        /// Runs a DB action inside a scope
        /// </summary>
        protected async Task WithDbAsync(Func<ApplicationDbContext, Task> action)
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await action(db);
        }

        /// <summary>
        /// Runs a DB function inside a scope
        /// </summary>
        protected async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> func)
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await func(db);
        }

        /// <summary>
        /// Runs a DB action synchronously inside a scope
        /// </summary>
        protected void WithDb(Action<ApplicationDbContext> action)
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            action(db);
        }

        /// <summary>
        /// Wipes the database tables to prevent test failures due to duplicate test data, like duplicate tag slug names 
        /// </summary>
        /// <returns></returns>
        protected async Task ResetDatabaseAsync()
        {
            await WithDbAsync(async db =>
            {
                db.StoryTags.RemoveRange(db.StoryTags);
                db.Stories.RemoveRange(db.Stories);
                db.Tags.RemoveRange(db.Tags);
                db.StoryActors.RemoveRange(db.StoryActors);
                db.StorySeries.RemoveRange(db.StorySeries);
                db.ChapterContents.RemoveRange(db.ChapterContents);
                db.Chapters.RemoveRange(db.Chapters);
                db.Actors.RemoveRange(db.Actors);

                await db.SaveChangesAsync();
            });
        }

        protected void AssertStartsWithItemsComeFirst(IEnumerable<string> names, string query)
        {
            var q = query.Trim();
            bool sawNonStartsWith = false;

            foreach (var name in names)
            {
                var starts = name.StartsWith(q, StringComparison.OrdinalIgnoreCase);

                if (!starts)
                {
                    sawNonStartsWith = true;
                    continue;
                }

                // If we see a starts-with AFTER we've already seen non-starts-with then the ordering is wrong
                Assert.False(sawNonStartsWith, $"Ordering violated: '{name}' starts with '{q}' but appeared after a non-starts-with result.");
            }
        }
    }
}
