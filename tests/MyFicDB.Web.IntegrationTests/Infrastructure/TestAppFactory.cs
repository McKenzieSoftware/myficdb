using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MyFicDB.Core;
using MyFicDB.Core.Interceptors;
using System.Data.Common;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MyFicDB.Web.IntegrationTests.Infrastructure;

public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;
    private string? _tempRoot;
    private readonly bool _enableAuth;

    // one public, one internal to resolve two seperate issues
    // issue 1: MyFicDB.Web.IntegrationTests.Infrastructure.TestAppFactory' had one or more unresolved constructor arguments: Boolean enableAuth
    // issue 2: Error Message: Class fixture type 'MyFicDB.Web.IntegrationTests.Infrastructure.TestAppFactory' may only define a single public constructor.
    public TestAppFactory()
    {
        _enableAuth = true;
    }

    internal TestAppFactory(bool enableAuth)
    {
        _enableAuth = enableAuth;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        _tempRoot = Path.Combine(Path.GetTempPath(), "MyFicDB.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var dbDir = Path.Combine(_tempRoot, "database");
        var logDir = Path.Combine(_tempRoot, "logs");
        Directory.CreateDirectory(dbDir);
        Directory.CreateDirectory(logDir);

        // GUARANTEED: Program.Main reads these via builder.Configuration very early.
        Environment.SetEnvironmentVariable("MYFICDB_DB_PATH", dbDir);
        Environment.SetEnvironmentVariable("MYFICDB_LOGS_PATH", logDir);
        Environment.SetEnvironmentVariable("MYFICDB_RESET_PASSWORD", "false");
        Environment.SetEnvironmentVariable("MYFICDB_SQLITE_COMMAND_TIMEOUT", "30");

        builder.ConfigureServices(services =>
        {
            // Remove app DB registrations
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));

            // Single in-memory SQLite connection for the lifetime of the test server
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // remove hosted services to prevent them running in paralel 
            services.RemoveAll<IHostedService>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options
                    .UseSqlite(_connection, sqlite =>
                    {;
                        sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    })
                    .AddInterceptors(sp.GetRequiredService<SqlitePragmaConnectionInterceptor>());
            });

            // Fake auth
            // this is enabled by default, but can be disabled for using an AnonymousClient
            // for [Authorize] testing
            if (_enableAuth)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = FakeAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = FakeAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(FakeAuthHandler.SchemeName, _ => { });
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // For tests, we use EnsureCreated but can switch to Migrate later to test migrations
        db.Database.EnsureCreated();
        // db.Database.Migrate();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;

            if (!string.IsNullOrWhiteSpace(_tempRoot) && Directory.Exists(_tempRoot))
            {
                try { Directory.Delete(_tempRoot, recursive: true); } catch { /* ignore */ }
            }
        }
    }
}
