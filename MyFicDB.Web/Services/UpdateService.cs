using MyFicDB.Core.Configuration;
using MyFicDB.Web.Areas.SystemManagement.ViewModels;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyFicDB.Web.Services
{
    public sealed class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly HttpClient _httpClient;
        private readonly BuildInfo _buildInfo;

        public UpdateService(ILogger<UpdateService> logger, HttpClient httpClient, BuildInfo buildInfo)
        {
            _logger = logger;
            _httpClient = httpClient;
            _buildInfo = buildInfo;
        }

        public async Task<SystemManagementUpdate> GetLatestReleaseAsync(string owner = "mckenziesoftware", string repo = "myficdb", CancellationToken cancellationToken = default)
        {
            var buildInfo = _buildInfo;

            if(string.IsNullOrEmpty(buildInfo.Version) || string.IsNullOrEmpty(buildInfo.BuildDate) || string.IsNullOrEmpty(buildInfo.GitSha))
            {
                _logger.LogError("Unable to get one more values from BuildInfo, {buildInfo}.  Cancelling.", buildInfo);
                return new SystemManagementUpdate
                {
                    InstalledVersion = "ERR",
                    InstalledBuildDate = "ERR",
                    InstalledGitSha = "ERR",
                    LatestVersion = "ERR",
                    IsUpdateAvailable = false,
                    ReleaseUrl = string.Empty,
                    ReleaseContent = $"Unable to check for update due to internal buildinfo failure."
                };
            }

            // get latest release from repo
            var latestReleaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            _logger.LogInformation("Checking for updates against: {latestReleaseUrl}", latestReleaseUrl);

            using var req = new HttpRequestMessage(HttpMethod.Get, latestReleaseUrl);

            // api requires user agent, also forcing json response
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("MyFicDB", buildInfo.Version.Length == 0 ? "0.0.0" : buildInfo.Version));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // 404
            if(res.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("GitHub releases/latest returned 404 for {owner}/{repo}.", owner, repo);

                return new SystemManagementUpdate
                {
                    InstalledVersion = buildInfo.Version,
                    InstalledBuildDate = buildInfo.BuildDate,
                    InstalledGitSha = buildInfo.GitSha,
                    LatestVersion = "ERR",
                    IsUpdateAvailable = false,
                    ReleaseUrl = string.Empty,
                    ReleaseContent = $"GitHub returned 404 for {owner}/{repo}."
                };
            }

            // any other error
            if(!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GitHub releases/latest failed: {StatusCode}. Body: {Body}", (int)res.StatusCode, body);

                return new SystemManagementUpdate
                {
                    InstalledVersion = buildInfo.Version,
                    InstalledBuildDate = buildInfo.BuildDate,
                    InstalledGitSha = buildInfo.GitSha,
                    LatestVersion = "ERR",
                    IsUpdateAvailable = false,
                    ReleaseUrl = string.Empty,
                    ReleaseContent = $"Update check failed ({(int)res.StatusCode}."
                };
            }

            await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (release is null)
            {
                return new SystemManagementUpdate
                {
                    InstalledVersion = buildInfo.Version,
                    InstalledBuildDate = buildInfo.BuildDate,
                    InstalledGitSha = buildInfo.GitSha,
                    LatestVersion = "ERR",
                    IsUpdateAvailable = false,
                    ReleaseUrl = string.Empty,
                    ReleaseContent = $"Update check failed, invalid response."
                };
            }

            var latestTag = NormalizeTagToVersion(release.TagName);
            var installedNorm = NormalizeTagToVersion(buildInfo.Version);

            var updateAvailable = IsNewer(latestTag, installedNorm);

            return new SystemManagementUpdate
            {
                InstalledVersion = installedNorm,
                InstalledBuildDate = buildInfo.BuildDate,
                InstalledGitSha = buildInfo.GitSha,
                LatestVersion = latestTag,
                IsUpdateAvailable = updateAvailable,
                ReleaseUrl = release.HtmlUrl ?? "",
                ReleaseContent = release.Body ?? "No Content"
            };

        }
    
        // rec
        private sealed class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }
        }

        // helpers
        internal static string NormalizeTagToVersion(string? input)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var v = input.Trim();

            // Incase of "v0.0.0"
            if(v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                v = v.Substring(1);
            }

            return v;
        }
    
        internal static bool IsNewer(string latest, string installed)
        {
            // If we can't parse, it's not newer
            // major.minor.patch comparison
            // if installed is somehow unknown, assume update available

            if (!VersionTryParse3(latest, out var latestV))
            {
                return false;
            }

            if (!VersionTryParse3(installed, out var installedV))
            {
                return true;
            }

            return latestV > installedV;
        }

        internal static bool VersionTryParse3(string input, out Version version)
        {
            version = new Version(0, 0, 0);

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            // Strip any prerelease suffix major.minor.patch-beta1
            var core = input.Split('-', 2)[0];

            // Ensure at least 3 components
            var parts = core.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            int major = 0, minor = 0, patch = 0;
            if (!int.TryParse(parts[0], out major))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out minor))
            {
                return false;
            }

            if (parts.Length >= 3)
            {
                int.TryParse(parts[2], out patch);
            }

            version = new Version(major, minor, patch);
            return true;
        }
    }
}
