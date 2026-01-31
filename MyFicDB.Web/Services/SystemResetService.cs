using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;

namespace MyFicDB.Web.Services
{
    /// <summary>
    /// <para>
    /// Service used for resetting the database back to an original unused state
    /// </para>
    /// <para>
    /// Once activated, <c>EnsureCreatedAsync</c> runs.  Once completed, <c>MigrateAsync</c> runs to re-create the database.
    /// </para>
    /// </summary>
    public sealed class SystemResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemResetService> _logger;

        public SystemResetService(ApplicationDbContext context, ILogger<SystemResetService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Nuculer: Deletes the entire database and creates again based on the migrations
        /// </summary>
        public async Task ActivateNuke(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Attempting to reset system");
                _logger.LogInformation($"Droping database...");

                await _context.Database.EnsureDeletedAsync(cancellationToken);

                _logger.LogInformation($"Recreating database from available migrations");
                await _context.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation($"Database should now be reset.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nuke activation failed.  System has not been reset.");
            }
        }
    }
}
