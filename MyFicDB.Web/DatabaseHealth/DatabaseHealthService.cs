using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Configuration;
using MyFicDB.Core.Extensions;

namespace MyFicDB.Web.DatabaseHealth
{
    /// <summary>
    /// Service is used for checking health related stuff in relation to the SQLite database used by da app
    /// </summary>
    public sealed class DatabaseHealthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseHealthService> _logger;
        private DateTimeOffset? _lastOkUtc;

        private readonly Directories _directories;

        public DatabaseHealthService(ApplicationDbContext context, Directories directories, ILogger<DatabaseHealthService> logger)
        {
            _context = context;
            _directories = directories;
            _logger = logger;
        }

        /// <summary>
        /// Runs a connection test, integrity check and gets the database size
        /// </summary>
        public async Task<(bool CanConnect, string Integrity, string SizeFormatted, DateTimeOffset? LastOkUtc)> CheckAsync(CancellationToken cancellationToken)
        {
            var canConnect = await CanOpenConnectionAsync(cancellationToken);

            var integrity = await RunIntegrityCheckAsync(cancellationToken);

            long size = File.Exists(_directories.DatabasePath) ? new FileInfo(_directories.DatabasePath).Length : 0;

            if (canConnect && string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
            {
                _lastOkUtc = DateTimeOffset.UtcNow;
            }

            return (canConnect, integrity, size.FormatFileSize(), _lastOkUtc);
        }

        /// <summary>
        /// Checks if we can open the database up
        /// </summary>
        private async Task<bool> CanOpenConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using (var conn = _context.Database.GetDbConnection())
                {
                    if (conn == null)
                    {
                        _logger.LogError("Connection state is null");
                        return false;
                    }

                    if (conn.State != System.Data.ConnectionState.Open)
                    {
                        await conn.OpenAsync(cancellationToken);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during connection check");
                return false;
            }
        }

        /// <summary>
        /// Checks integrity of the database, also checks connection
        /// </summary>
        private async Task<string> RunIntegrityCheckAsync(CancellationToken cancellationToken)
        {
            try
            {

                await using (var conn = _context.Database.GetDbConnection())
                {
                    if(conn == null)
                    {
                        _logger.LogError("Connection state is null");
                        return "FAIL";
                    }

                    if(conn.State != System.Data.ConnectionState.Open)
                    {
                        await conn.OpenAsync(cancellationToken);
                    }

                    await using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "PRAGMA integrity_check;";

                        var result = await command.ExecuteScalarAsync(cancellationToken);
                        return (result?.ToString() ?? "unknown").Trim();
                    }

                }

            } catch (Exception ex)
            {
                return ex.Message;
            }
        }
    
    }
}
