using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Configuration;
using MyFicDB.Core.DatabaseHealth;
using MyFicDB.Core.Interceptors;
using MyFicDB.Exporter.Interfaces;
using MyFicDB.Exporter.Services;
using MyFicDB.Web.Areas.SystemManagement.ViewModels;
using MyFicDB.Web.DatabaseHealth;
using MyFicDB.Web.Options;
using MyFicDB.Web.Services;
using Serilog;
using System.Text.Json;

namespace MyFicDB.Web
{
    public class Program
    {
        /// <summary>
        /// This is the output template used for Serilog logging, used by .Console and .File
        /// </summary>
        private const string OUTPUT_TEMPLATE = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Default directories
            var defaultDbDir = "/myficdb-test/database";
            var defaultLogDir = "/myficdb-test/logs";

            var dbDir = EnsureDirectory(builder.Configuration["MYFICDB_DB_PATH"], defaultDbDir, "MYFICDB_DB_PATH", builder.Environment);
            var logDir = EnsureDirectory(builder.Configuration["MYFICDB_LOGS_PATH"], defaultLogDir, "MYFICDB_LOGS_PATH", builder.Environment);

            // generate db path
            var dbPath = Path.Combine(dbDir, "myficdb.db");

            // Database config
            var dbCommandTimeout = builder.Configuration.GetValue("MYFICDB_SQLITE_COMMAND_TIMEOUT", 30);

            // Check if password reset is enabled
            builder.Services.Configure<ResetPasswordOptions>(o =>
            {
                o.Enabled = builder.Configuration.GetValue<bool>("MYFICDB_RESET_PASSWORD");
                o.NewPassword = builder.Configuration["MYFICDB_RESET_PASSWORD_VALUE"];
            });

            // Configure serilog
            builder.Host.UseSerilog((ctx, cfg) =>
            {
                cfg
                    .ReadFrom.Configuration(ctx.Configuration)
                    .Enrich
                        .FromLogContext()
                    .WriteTo
                        .Console(
                            outputTemplate: OUTPUT_TEMPLATE)
                    .WriteTo
                        .File(
                            Path.Combine(logDir, "myficdb-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 14,
                            shared: true,
                            outputTemplate: OUTPUT_TEMPLATE
                        );
            });

            var connectionString = $"Data Source={dbPath}";

            // This is required for the dbcontext below
            builder.Services.AddSingleton<SqlitePragmaConnectionInterceptor>();

            // For getting direcotires across the app
            builder.Services.AddSingleton(new Directories(Logs: logDir, Database: dbDir, DatabasePath: dbPath));

            // Load licence.json info once
            builder.Services.AddSingleton(provider =>
            {
                var env = provider.GetRequiredService<IWebHostEnvironment>();
                var path = Path.Combine(env.WebRootPath, "licences", "licences.json");

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<NuGetLicenceViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<NuGetLicenceViewModel>();
            });

            // Configure the dbcontext
            builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options
                    .UseSqlite(connectionString, sqlite =>
                    {
                        sqlite.CommandTimeout(dbCommandTimeout);
                        sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    })
                    .AddInterceptors(sp.GetRequiredService<SqlitePragmaConnectionInterceptor>());
            });

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddControllersWithViews();

            // Data Protection, single-user, locally contained app, can just store it in the database.  shouldnt be an issue
            // 08/01/26 - DISABLED IN TESTS
            var dataProtection = builder.Services
                .AddDataProtection()
                .SetApplicationName("MyFicDB");

            if (!builder.Environment.IsEnvironment("Testing"))
            {
                dataProtection.PersistKeysToDbContext<ApplicationDbContext>();
            }

            // force urls to be lowercase
            builder.Services.AddRouting(options => options.LowercaseUrls = true);

            // change identity urls/page routes
            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Login", "/login");
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Register", "/register");
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Manage/Index", "/account");
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Manage/ChangePassword", "/account/change-password");
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Manage/TwoFactorAuthentication", "/account/2fa");
                options.Conventions.AddAreaPageRoute("Identity", "/Account/LoginWith2fa", "/login-with-2fa");
                options.Conventions.AddAreaPageRoute("Identity", "/Account/LoginWithRecoverCode", "/login-with-recovery");
            });

            // update cookies to account for new urls above
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/access-denied";
            });

            // Services
            builder.Services.AddScoped<HtmlSanitizationService>();
            builder.Services.AddScoped<TagService>();
            builder.Services.AddScoped<ActorService>();
            builder.Services.AddScoped<SeriesService>();
            builder.Services.AddScoped<StoryRelationshipService>();
            builder.Services.AddScoped<SystemResetService>();
            builder.Services.AddScoped<IStoryExportService, StoryExportService>();

            // Http Service
            builder.Services.AddHttpClient<UpdateService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // Database Health Service
            builder.Services.AddScoped<DatabaseHealthService>();
            builder.Services.AddSingleton<IDatabaseHealthSnapshotStore, DatabaseHealthSnapshotStore>();
            builder.Services.AddHostedService<DatabaseHealthHostedService>();

            // for reseting passwords, based on env var being set to true
            builder.Services.AddHostedService<ResetPasswordHostedService>();

            // build info
            builder.Services.AddSingleton(new BuildInfo(
                Environment.GetEnvironmentVariable("APP_VERSION"),
                Environment.GetEnvironmentVariable("GIT_SHA"),
                Environment.GetEnvironmentVariable("BUILD_DATE")
            ));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // set environment to testing in the MyFicDB.Web.Tests.  This should prevent Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]#
            // as it's just an annoying warning that's irrelevant to the test[s]
            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var pending = await db.Database.GetPendingMigrationsAsync();

                if(pending.Any())
                {
                    // migrate
                    db.Database.Migrate();
                }

            }

            app.Run();
        }

        static string EnsureDirectory(string? pathFromConfig, string fallback, string nameForErrors, IHostEnvironment environment)
        {
            var path = string.IsNullOrWhiteSpace(pathFromConfig) ? fallback : pathFromConfig;

            // remove surrounding quotes and whitespace
            path = path.Trim().Trim('"');

            // If the user accidentally provides a file path, convert to directory.
            // (e.g. /myficdb-test/database/myficdb.db)
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path) ?? path;
            }

            // Make absolute inside container if user gives relative.
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, path);
            }

            Directory.CreateDirectory(path);

            // validate it is writable unless we're in test env, 
            // if we run this in the test projs it shits its wee pants
            if (!environment.IsEnvironment("Testing"))
            {
                var testFile = Path.Combine(path, ".write-test");
                File.WriteAllText(testFile, "ok");
                File.Delete(testFile);
            }

            return path;
        }

    }
}
