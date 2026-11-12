using libraryMVC.DTOs.Reports;

namespace libraryMVC.Interfaces;

public interface IReportService
{
    Task<ReportFile> GenerateAsync(
        ReportEntity entity,
        ReportFormat format,
        CancellationToken cancellationToken = default,
        ReportFilters? filters = null);
}
