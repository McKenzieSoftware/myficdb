namespace MyFicDB.Web.DatabaseHealth
{
    public sealed class DatabaseHealthHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDatabaseHealthSnapshotStore _databaseHealthSnapshotStore;
        private readonly ILogger<DatabaseHealthHostedService> _logger;
        private readonly TimeSpan _interval;

        public DatabaseHealthHostedService(IServiceScopeFactory serviceScopeFactory, IDatabaseHealthSnapshotStore databaseHealthSnapshotStore, ILogger<DatabaseHealthHostedService> logger, TimeSpan? interval = null)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _databaseHealthSnapshotStore = databaseHealthSnapshotStore;
            _logger = logger;
            _interval = interval ?? TimeSpan.FromHours(12);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run once immediately on startup
            await RunOnce(stoppingToken);

            // configure timer to run per defined interval in constructor
            using var timer = new PeriodicTimer(_interval);

            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnce(stoppingToken);
            }
        }

        private async Task RunOnce(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<DatabaseHealthService>();

                var (canConnect, integrity, sizeFormatted, lastOkUtc) = await svc.CheckAsync(stoppingToken);

                var snapshot = new DatabaseHealthSnapshotRecord(
                    CanConnect: canConnect,
                    Integrity: integrity,
                    SizeFormatted: sizeFormatted,
                    LastOkUtc: lastOkUtc,
                    CheckedAtUtc: DateTimeOffset.UtcNow,
                    Error: null
                );

                _databaseHealthSnapshotStore.Set(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed.");

                // Preserve last-known-good fields
                var snapshot = new DatabaseHealthSnapshotRecord(
                    CanConnect: false,
                    Integrity: "FAIL",
                    SizeFormatted: "0B",
                    LastOkUtc: _databaseHealthSnapshotStore.Current?.LastOkUtc,
                    CheckedAtUtc: DateTimeOffset.UtcNow,
                    Error: ex.Message
                );

                _databaseHealthSnapshotStore.Set(snapshot);
            }
        }
    }
}
