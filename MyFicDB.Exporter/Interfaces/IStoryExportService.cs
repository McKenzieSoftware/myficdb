using Microsoft.AspNetCore.Mvc;
using MyFicDB.Exporter.Enums;

namespace MyFicDB.Exporter.Interfaces
{
    public interface IStoryExportService
    {
        Task<FileContentResult> ExportStoryAsync(Guid storyId, StoryExportType type, CancellationToken cancellationToken = default);
        Task<FileContentResult> ExportAllStoriesAsHtmlZipAsync(CancellationToken cancellationToken = default);

        bool TryParseType(string type, out StoryExportType exportType);
    }
}
