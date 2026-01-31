using System.Text.Json.Serialization;

namespace MyFicDB.Web.Areas.SystemManagement.ViewModels
{
    public sealed class NuGetLicenceViewModel
    {
        [JsonPropertyName("PackageName")]
        public string PackageName { get; init; } = default!;

        [JsonPropertyName("PackageVersion")]
        public string PackageVersion { get; init; } = default!;

        [JsonPropertyName("PackageUrl")]
        public string PackageUrl { get; init; } = default!;

        [JsonPropertyName("Copyright")]
        public string Copyright { get; init; } = default!;

        [JsonPropertyName("Authors")]
        public List<string> Authors { get; init; } = new();

        [JsonPropertyName("Description")]
        public string Description { get; init; } = default!;

        [JsonPropertyName("LicenseUrl")]
        public string LicenceUrl { get; init; } = default!;

        [JsonPropertyName("LicenseType")]
        public string LicenceType { get; init; } = default!;

        [JsonPropertyName("Repository")]
        public NuGetRepositoryViewModel? Repository { get; init; }


        public string DisplayName => $"{PackageName} {PackageVersion}";

        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
        public bool HasProjectUrl => !string.IsNullOrWhiteSpace(PackageUrl);
        public bool HasLicenceUrl => !string.IsNullOrWhiteSpace(LicenceUrl);
        public bool HasRepository => Repository is not null && Repository.HasUrl;
    }
}
