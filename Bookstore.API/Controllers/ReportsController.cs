using Bookstore.Application.Features.Reports.Queries;
using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
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
    private readonly IMediator _mediator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IMediator mediator, ILogger<ReportsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics and summary
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard report data</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DashboardReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating dashboard report via MediatR");
        var response = await _mediator.Send(new GetDashboardReportQuery(), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Get inventory and stock statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inventory report data</returns>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(ApiResponse<InventoryReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating inventory report via MediatR");
        var response = await _mediator.Send(new GetInventoryReportQuery(), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
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

        _logger.LogInformation("Exporting sales report from {Start} to {End} via MediatR", start, end);
        var response = await _mediator.Send(new ExportSalesReportQuery(start, end), cancellationToken);

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
    [ProducesResponseType(typeof(ApiResponse<UserEngagementReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserEngagementReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating user engagement report via MediatR");
        var response = await _mediator.Send(new GetUserEngagementReportQuery(), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Get review analytics and ratings statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Review analytics report data</returns>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(ApiResponse<ReviewAnalyticsReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewAnalyticsReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating review analytics report via MediatR");
        var response = await _mediator.Send(new GetReviewAnalyticsReportQuery(), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }
}
