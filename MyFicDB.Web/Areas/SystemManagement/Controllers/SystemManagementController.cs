using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Configuration;
using MyFicDB.Exporter.Interfaces;
using MyFicDB.Web.Areas.SystemManagement.ViewModels;
using MyFicDB.Web.Services;
using ReverseMarkdown.Converters;
using System.IO.Compression;
using System.Reflection;

namespace MyFicDB.Web.Areas.SystemManagement.Controllers
{
    [Authorize]
    [Area("SystemManagement")]
    [Route("system")]
    public sealed class SystemManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly List<NuGetLicenceViewModel> _licences;

        private const string DatabaseDownloadName = "MyFicDB-Database-"; // This is appended with a timestamp and the file extension
        
        private readonly IStoryExportService _storyExportService;
        private readonly SystemResetService _systemResetService;
        private readonly UpdateService _updateService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<SystemManagementController> _logger;

        private readonly Directories _directories;
        private readonly BuildInfo _buildInfo;

        public SystemManagementController(
            List<NuGetLicenceViewModel> licences, ApplicationDbContext context, IStoryExportService storyExportService,
            UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, SystemResetService systemResetService,
            UpdateService updateService, ILogger<SystemManagementController> logger, Directories directories, BuildInfo buildInfo
        )
        { 
            _licences = licences;
            _context = context;
            _storyExportService = storyExportService;
            _userManager = userManager;
            _signInManager = signInManager;
            _systemResetService = systemResetService;
            _updateService = updateService;
            _logger = logger;
            _directories = directories;
            _buildInfo = buildInfo;
        }
        
        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var version = _buildInfo.Version;

            // get system logs, limited to 10
            var logs = new DirectoryInfo(_directories.Logs)
                .GetFiles("*.log", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Select(f => new SystemManagementLogs
                {
                    FileName = f.Name,
                    SizeBytes = f.Length,
                    LastWriteUtc = f.LastWriteTimeUtc
                })
                .Take(10)
                .ToList();

            var systemStartTimeUtc = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var stories = _context.Stories.AsNoTracking();

            var vm = new SystemManagementViewModel
            {
                UpdateInformation = await _updateService.GetLatestReleaseAsync(cancellationToken: cancellationToken),
                Logs = logs,
                SystemInfo = new SystemManagementInformation
                {
                    StoriesTotal = stories.Count(),
                    TagsTotal = _context.Tags.AsNoTracking().Count(),
                    SeriesTotal = _context.Series.AsNoTracking().Count(),
                    ActorsTotal = _context.Actors.AsNoTracking().Count(),
                    Uptime = "Loading...",
                    StartTime = systemStartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                    NutTotal = stories
                        .Select(s => s.NutCounter)
                        .Sum(),
                    ReadTotal = stories
                        .Select(s => s.ReadCounter)
                        .Sum(),
                    NsfwTotal = stories
                        .Select(s => s.IsNsfw)
                        .Count()
                }
            };

            return View(vm);
        }

        [HttpGet("log/raw/{fileName}")]
        public IActionResult LogRaw(string fileName)
        {
            if(!fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected file as it is a non-log request: {file}", fileName);
            }

            var fullPath = Path.GetFullPath(Path.Combine(_directories.Logs, fileName));

            if(!fullPath.StartsWith(_directories.Logs, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected file as the resolved path is not within the designated logs directory: {file}", fileName);
                return BadRequest();
            }

            if(!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Rejected as file cannot be found in the logs path: {file}", fileName);
                return BadRequest();
            }

            return PhysicalFile(fullPath, "text/plain; charset=utf-8");
        }

        [HttpGet("reset-system")]
        public IActionResult Reset()
        {
            return View(new ResetSystemViewModel());
        }

        [HttpPost("reset-system")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(ResetSystemViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Unauthorized();
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, viewModel.Password);
            if (!passwordValid)
            {
                _logger.LogInformation("Incorrect password entered when trying to reset system.");

                ModelState.AddModelError(nameof(viewModel.Password), "Incorrect password.");
                return View(viewModel);
            }

            _logger.LogInformation("Correct password supplied during System Reset, activating nuke.");

            await _systemResetService.ActivateNuke(cancellationToken);

            _logger.LogInformation("Logging user out of the system.");

            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost("download-database")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadDatabase()
        {
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return BadRequest("Database connection is not configured.");
            }

            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var tempDir = Path.Combine(Path.GetTempPath(), "MyFicDB");
            Directory.CreateDirectory(tempDir);

            var sqliteName = $"{DatabaseDownloadName}{stamp}.sqlite";
            var sqlitePath = Path.Combine(tempDir, sqliteName);

            var zipName = $"{DatabaseDownloadName}{stamp}.zip";
            var zipPath = Path.Combine(tempDir, zipName);

            // Ensure old files aren't hanging around
            SafeDelete(sqlitePath);
            SafeDelete(zipPath);

            // Create a consistent backup
            await using (var source = new SqliteConnection(connectionString))
            {
                await using (var dest = new SqliteConnection($"Data Source={sqlitePath};Mode=ReadWriteCreate;Cache=Default;"))
                {
                    await source.OpenAsync();
                    await dest.OpenAsync();

                    // Backup while live
                    source.BackupDatabase(dest);

                    // Force close (throws weird issues on windows if this isn't done, so don't remove it)
                    await dest.CloseAsync();
                    await source.CloseAsync();
                }
            }

            // Zip it with retry + shared-read stream to avoid transient locks
            await CreateZipWithRetryAsync(zipPath, sqlitePath, sqliteName);

            // Stream back the zip and clean up afterwards
            var zipStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            HttpContext.Response.OnCompleted(() =>
            {
                zipStream.Dispose();
                SafeDelete(sqlitePath);
                SafeDelete(zipPath);
                return Task.CompletedTask;
            });

            return File(zipStream, "application/zip", zipName);
        }

        [HttpPost("export")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportAllStories(CancellationToken cancellationToken)
        {
            var file = await _storyExportService.ExportAllStoriesAsHtmlZipAsync(cancellationToken);
            return file;
        }

        [HttpGet("licences")]
        public IActionResult Licences()
        {
            return View(_licences.OrderBy(l => l.PackageName).ToList());
        }

        // redirect for EN-US spelling to EN-GB
        [HttpGet("licenses")]
        public IActionResult Licenses()
        {
           return RedirectToAction(nameof(Licences));
        }

        #region Download Database Helpers
        private static async Task CreateZipWithRetryAsync(string zipPath, string sqlitePath, string entryName)
        {
            const int maxAttempts = 8;
            const int delayMs = 75;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

                    // Read the sqlite file via stream with sharing to avoid in use failures
                    using var sqliteStream = new FileStream(
                        sqlitePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete);

                    var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await sqliteStream.CopyToAsync(entryStream);

                    return; // success
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs);
                }
            }

            // Final attempt throws if still failing
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                using (var sqliteStream = new FileStream(
                           sqlitePath,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete))
                {
                    var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    sqliteStream.CopyTo(entryStream);
                }
            }
        }
        private static void SafeDelete(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch { /* ignore */ }
        }
        #endregion
    }
}
