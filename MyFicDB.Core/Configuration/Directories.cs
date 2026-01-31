namespace MyFicDB.Core.Configuration
{
    /// <summary>
    /// Globally used application directories
    /// </summary>
    /// <param name="Logs">
    /// Log path without file name
    /// </param>
    /// <param name="Database">
    /// Database path without file name
    /// </param>
    /// <param name="DatabasePath">
    /// Full database path with file name
    /// </param>
    public sealed record Directories(string Logs, string Database, string DatabasePath);
}
