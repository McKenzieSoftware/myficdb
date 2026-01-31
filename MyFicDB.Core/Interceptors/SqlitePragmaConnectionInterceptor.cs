using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;


namespace MyFicDB.Core.Interceptors
{
    public class SqlitePragmaConnectionInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            ApplyPragmas(connection);
            base.ConnectionOpened(connection, eventData);
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            await ApplyPragmasAsync(connection, cancellationToken);
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        private static void ApplyPragmas(DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA busy_timeout = 5000;
PRAGMA temp_store = MEMORY;
PRAGMA cache_size = -32768;
PRAGMA mmap_size = 268435456;
PRAGMA case_sensitive_like = OFF;
";
            cmd.ExecuteNonQuery();
        }

        private static async Task ApplyPragmasAsync(DbConnection connection, CancellationToken ct)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA busy_timeout = 5000;
PRAGMA temp_store = MEMORY;
PRAGMA cache_size = -32768;
PRAGMA mmap_size = 268435456;
PRAGMA case_sensitive_like = OFF;
";
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
