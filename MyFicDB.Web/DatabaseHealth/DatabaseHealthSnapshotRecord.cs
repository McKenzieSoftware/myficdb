namespace MyFicDB.Web.DatabaseHealth
{
    public sealed record DatabaseHealthSnapshotRecord(
        bool CanConnect,
        string Integrity,
        string SizeFormatted,
        DateTimeOffset? LastOkUtc,
        DateTimeOffset CheckedAtUtc,
        string? Error
    );
}
