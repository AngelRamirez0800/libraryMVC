using libraryMVC.DTOs.Lists;
using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;
using libraryMVC.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers;

[Authorize(Roles = RoleNames.BookManagers)]
[Route("[controller]")]
public sealed partial class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("books/pdf")]
    public Task<IActionResult> BooksPdf(
        [FromQuery] BookListFilter filters,
        CancellationToken cancellationToken) =>
        DownloadAsync(
            ReportEntity.Books,
            ReportFormat.Pdf,
            new ReportFilters(Search: filters.Search, AuthorId: filters.AuthorId, CategoryId: filters.CategoryId),
            cancellationToken);

    [HttpGet("books/excel")]
    public Task<IActionResult> BooksExcel(
        [FromQuery] BookListFilter filters,
        CancellationToken cancellationToken) =>
        DownloadAsync(
            ReportEntity.Books,
            ReportFormat.Excel,
            new ReportFilters(Search: filters.Search, AuthorId: filters.AuthorId, CategoryId: filters.CategoryId),
            cancellationToken);

    [HttpGet("authors/pdf")]
    public Task<IActionResult> AuthorsPdf(
        [FromQuery] AuthorListFilter filters,
        CancellationToken cancellationToken) =>
        DownloadAsync(
            ReportEntity.Authors,
            ReportFormat.Pdf,
            new ReportFilters(Search: filters.Search, Nationality: filters.Nationality),
            cancellationToken);

    [HttpGet("authors/excel")]
    public Task<IActionResult> AuthorsExcel(
        [FromQuery] AuthorListFilter filters,
        CancellationToken cancellationToken) =>
        DownloadAsync(
            ReportEntity.Authors,
            ReportFormat.Excel,
            new ReportFilters(Search: filters.Search, Nationality: filters.Nationality),
            cancellationToken);

    [HttpGet("categories/pdf")]
    public Task<IActionResult> CategoriesPdf(
        [FromQuery] CategoryListFilter filters,
        CancellationToken cancellationToken) =>
        DownloadAsync(
            ReportEntity.Categories,
            ReportFormat.Pdf,
            new ReportFilters(Search: filters.Search),
            cancellationToken);

    [HttpGet("categories/excel")]
    public Task<IActionResult> CategoriesExcel(
        [FromQuery] CategoryListFilter filters,
        CancellationToken cancellationToken) =>
        DownloadAsync(
            ReportEntity.Categories,
            ReportFormat.Excel,
            new ReportFilters(Search: filters.Search),
            cancellationToken);

    private async Task<IActionResult> DownloadAsync(
        ReportEntity entity,
        ReportFormat format,
        ReportFilters filters,
        CancellationToken cancellationToken)
    {
        LogReportGeneration(_logger, entity, format);
        ReportFile report = await _reportService.GenerateAsync(entity, format, cancellationToken, filters);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Generating {entity} report in {format} format.")]
    private static partial void LogReportGeneration(
        ILogger logger,
        ReportEntity entity,
        ReportFormat format);
}
