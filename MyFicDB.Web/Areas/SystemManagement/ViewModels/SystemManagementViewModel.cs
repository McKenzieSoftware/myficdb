namespace MyFicDB.Web.Areas.SystemManagement.ViewModels
{
    public sealed class SystemManagementViewModel()
    {
        // Version Section
        public SystemManagementUpdate UpdateInformation { get; init; } = new();

        // Logs Section
        public List<SystemManagementLogs> Logs { get; init; } = new();

        // System Information
        public SystemManagementInformation? SystemInfo { get; init; }

        // Database Health Check
        public SystemManagementDatabaseHealth? DatabaseHealth { get; init; }
    }

    public sealed class SystemManagementLogs()
    {
        public string FileName { get; init; } = "";
        public long SizeBytes { get; init; }
        public DateTime LastWriteUtc { get; init; }
    }

    public sealed class SystemManagementInformation()
    {
        public string Uptime { get; init; } = "Unavailable";
        public string StartTime { get; init; } = "Unavailable";
        public int StoriesTotal { get; init; }
        public int TagsTotal { get; init; }
        public int SeriesTotal { get; init; }
        public int ActorsTotal { get; init; }
        public int NutTotal { get; init; }
        public int ReadTotal { get; init; }
        public int NsfwTotal { get; init; }
    }

    public sealed class SystemManagementDatabaseHealth()
    {
        public bool CanConnect { get; set; }
        public string? IntegrityResult { get; set; }
        public string? DatabaseSize { get; set; }
        public DateTimeOffset? LastSuccessfulCheckUtc { get; set; }
    }

    public sealed class SystemManagementUpdate()
    {
        public string InstalledVersion { get; init; } = "";
        public string InstalledBuildDate { get; init; } = "";
        public string InstalledGitSha { get; init; } = "";
        public string LatestVersion { get; init; } = "";
        public bool IsUpdateAvailable { get; init; } = false;
        public string ReleaseUrl { get; init; } = "";
        public string ReleaseContent { get; init; } = "";

    }
}
