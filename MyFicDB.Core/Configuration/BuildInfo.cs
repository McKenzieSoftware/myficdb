namespace MyFicDB.Core.Configuration
{
    public sealed record BuildInfo(string? Version, string? GitSha, string? BuildDate);
}
