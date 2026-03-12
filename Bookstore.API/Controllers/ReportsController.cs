using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.API.Controllers;

/// <summary>
/// Reporting and Analytics endpoints (Admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics and summary
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard report data</returns>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating dashboard report");
        var response = await _reportService.GetDashboardReportAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get inventory and stock statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inventory report data</returns>
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating inventory report");
        var response = await _reportService.GetInventoryReportAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Export sales report as CSV
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV file</returns>
    [HttpGet("export/sales")]
    public async Task<IActionResult> ExportSalesReport([FromQuery] DateTimeOffset? startDate, [FromQuery] DateTimeOffset? endDate, CancellationToken cancellationToken)
    {
        var start = startDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTimeOffset.UtcNow;

        _logger.LogInformation("Exporting sales report from {Start} to {End}", start, end);
        var response = await _reportService.ExportSalesReportAsync(start, end, cancellationToken);

        if (!response.Success || response.Data == null)
        {
            return StatusCode(response.StatusCode ?? 400, response);
        }

        return File(response.Data, "text/csv", $"sales_report_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Get user engagement statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User engagement report data</returns>
    [HttpGet("user-engagement")]
    public async Task<IActionResult> GetUserEngagementReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating user engagement report");
        var response = await _reportService.GetUserEngagementReportAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get review analytics and ratings statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Review analytics report data</returns>
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviewAnalyticsReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating review analytics report");
        var response = await _reportService.GetReviewAnalyticsReportAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }
}
