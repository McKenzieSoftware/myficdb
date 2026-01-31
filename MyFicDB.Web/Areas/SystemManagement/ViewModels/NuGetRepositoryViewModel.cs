using System.Text.Json.Serialization;

namespace MyFicDB.Web.Areas.SystemManagement.ViewModels
{
    public sealed class NuGetRepositoryViewModel
    {
        [JsonPropertyName("Type")]
        public string? Type { get; init; }

        [JsonPropertyName("Url")]
        public string? Url { get; init; }

        [JsonPropertyName("Commit")]
        public string? Commit { get; init; }

        public bool HasUrl => !string.IsNullOrWhiteSpace(Url);
        public bool HasCommit => !string.IsNullOrWhiteSpace(Commit);

        public string UrlNormalized => Url!.Replace("git://", "https://");
    }
}
