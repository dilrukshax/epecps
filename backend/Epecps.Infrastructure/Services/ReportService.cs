using Epecps.Application.DTOs.Reports;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service for generating HR reports
/// </summary>
public class ReportService : IReportService
{
    private readonly EpecpsDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(EpecpsDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
        // Set EPPlus license context (for free non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<List<EvaluationReportDataDto>> GetEvaluationReportDataAsync(
        EvaluationReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting GetEvaluationReportDataAsync with filters: CycleId={CycleId}, DepartmentId={DepartmentId}, Status={Status}", 
                filter.CycleId, filter.DepartmentId, filter.Status);

            var query = _context.Evaluations
                .Include(e => e.Employee)
                    .ThenInclude(u => u.Department)
                .Include(e => e.Cycle)
                .Include(e => e.ReportingManager)
                .Include(e => e.TeamLead)
                .Include(e => e.Reviews)
                    .ThenInclude(r => r.ReviewItems)
                .Include(e => e.PromotionCases)
                .AsQueryable();

            // Log total count before filters
            var totalCount = await query.CountAsync(cancellationToken);
            _logger.LogInformation("Total evaluations in database: {TotalCount}", totalCount);

            // Apply filters
            if (filter.CycleId.HasValue)
            {
                query = query.Where(e => e.CycleId == filter.CycleId.Value);
                _logger.LogInformation("Filtering by CycleId: {CycleId}", filter.CycleId.Value);
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(e => e.Employee.DeptId == filter.DepartmentId.Value);
                _logger.LogInformation("Filtering by DepartmentId: {DepartmentId}", filter.DepartmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(e => e.Status.ToLower() == filter.Status.ToLower());
                _logger.LogInformation("Filtering by Status: {Status}", filter.Status);
            }

            if (filter.OnlyPromoted.HasValue && filter.OnlyPromoted.Value)
            {
                query = query.Where(e => e.PromotionCases.Any(pc => pc.GmDecision == PromotionDecision.Approved));
                _logger.LogInformation("Filtering for promoted employees only");
            }

            if (filter.MinScore.HasValue)
            {
                query = query.Where(e => e.OverallScore >= filter.MinScore.Value);
                _logger.LogInformation("Filtering by MinScore: {MinScore}", filter.MinScore.Value);
            }

            if (filter.MaxScore.HasValue)
            {
                query = query.Where(e => e.OverallScore <= filter.MaxScore.Value);
                _logger.LogInformation("Filtering by MaxScore: {MaxScore}", filter.MaxScore.Value);
            }

            var evaluations = await query
                .OrderByDescending(e => e.EvaluationId)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} evaluations after applying filters", evaluations.Count);

            var reportData = evaluations.Select(e => new EvaluationReportDataDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeName = e.Employee.FullName,
                EmployeeEmail = e.Employee.Email,
                Department = e.Employee.Department.Name,
                CycleName = e.Cycle.Name,
                Status = e.Status,
                OverallScore = e.OverallScore,
                ReportingManagerName = e.ReportingManager.FullName,
                TeamLeadName = e.TeamLead.FullName,
                IsPromoted = e.PromotionCases.Any(pc => pc.GmDecision == PromotionDecision.Approved),
                PromotionStatus = GetPromotionStatus(e),
                SubmittedDate = GetSubmittedDate(e),
                CompletedDate = GetCompletedDate(e),
                
                // Extract review scores
                RmScore = GetReviewScore(e, ReviewerRole.RM),
                TlScore = GetReviewScore(e, ReviewerRole.TL),
                PeerScore1 = GetPeerScore(e, 0),
                PeerScore2 = GetPeerScore(e, 1),
                HodScore = GetReviewScore(e, ReviewerRole.HOD),
                GmScore = GetReviewScore(e, ReviewerRole.GM)
            }).ToList();

            _logger.LogInformation("Successfully created {Count} report data records", reportData.Count);
            return reportData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEvaluationReportDataAsync");
            throw;
        }
    }

    public async Task<byte[]> GenerateEvaluationExcelReportAsync(
        EvaluationReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var data = await GetEvaluationReportDataAsync(filter, cancellationToken);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Evaluation Report");

        // Header styling
        var headerRow = 1;
        var headerStyle = worksheet.Cells[headerRow, 1, headerRow, 16];
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerStyle.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
        headerStyle.Style.Font.Color.SetColor(Color.White);
        headerStyle.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        headerStyle.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        // Headers
        worksheet.Cells[headerRow, 1].Value = "Evaluation ID";
        worksheet.Cells[headerRow, 2].Value = "Employee Name";
        worksheet.Cells[headerRow, 3].Value = "Email";
        worksheet.Cells[headerRow, 4].Value = "Department";
        worksheet.Cells[headerRow, 5].Value = "Cycle";
        worksheet.Cells[headerRow, 6].Value = "Status";
        worksheet.Cells[headerRow, 7].Value = "Overall Score";
        worksheet.Cells[headerRow, 8].Value = "RM Score";
        worksheet.Cells[headerRow, 9].Value = "TL Score";
        worksheet.Cells[headerRow, 10].Value = "Peer 1 Score";
        worksheet.Cells[headerRow, 11].Value = "Peer 2 Score";
        worksheet.Cells[headerRow, 12].Value = "HOD Score";
        worksheet.Cells[headerRow, 13].Value = "GM Score";
        worksheet.Cells[headerRow, 14].Value = "Promoted";
        worksheet.Cells[headerRow, 15].Value = "Submitted Date";
        worksheet.Cells[headerRow, 16].Value = "Completed Date";

        // Data rows
        var row = 2;
        foreach (var item in data)
        {
            worksheet.Cells[row, 1].Value = item.EvaluationId;
            worksheet.Cells[row, 2].Value = item.EmployeeName;
            worksheet.Cells[row, 3].Value = item.EmployeeEmail;
            worksheet.Cells[row, 4].Value = item.Department;
            worksheet.Cells[row, 5].Value = item.CycleName;
            worksheet.Cells[row, 6].Value = item.Status;
            worksheet.Cells[row, 7].Value = item.OverallScore;
            worksheet.Cells[row, 8].Value = item.RmScore;
            worksheet.Cells[row, 9].Value = item.TlScore;
            worksheet.Cells[row, 10].Value = item.PeerScore1;
            worksheet.Cells[row, 11].Value = item.PeerScore2;
            worksheet.Cells[row, 12].Value = item.HodScore;
            worksheet.Cells[row, 13].Value = item.GmScore;
            worksheet.Cells[row, 14].Value = item.IsPromoted ? "Yes" : "No";
            worksheet.Cells[row, 15].Value = item.SubmittedDate?.ToString("yyyy-MM-dd");
            worksheet.Cells[row, 16].Value = item.CompletedDate?.ToString("yyyy-MM-dd");

            // Conditional formatting for promoted employees
            if (item.IsPromoted)
            {
                var promotedRow = worksheet.Cells[row, 1, row, 16];
                promotedRow.Style.Fill.PatternType = ExcelFillStyle.Solid;
                promotedRow.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 239, 206));
            }

            row++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        // Freeze header row
        worksheet.View.FreezePanes(2, 1);

        // Add filters
        worksheet.Cells[1, 1, row - 1, 16].AutoFilter = true;

        return package.GetAsByteArray();
    }

    private string? GetPromotionStatus(Evaluation evaluation)
    {
        var promotionCase = evaluation.PromotionCases.FirstOrDefault();
        if (promotionCase == null) return null;

        return promotionCase.GmDecision switch
        {
            PromotionDecision.Approved => "Approved",
            PromotionDecision.Rejected => "Rejected",
            _ => "Pending"
        };
    }

    private DateTime? GetSubmittedDate(Evaluation evaluation)
    {
        // Get the first review submission date
        return evaluation.Reviews
            .Where(r => r.SubmittedAt.HasValue)
            .OrderBy(r => r.SubmittedAt)
            .FirstOrDefault()?.SubmittedAt;
    }

    private DateTime? GetCompletedDate(Evaluation evaluation)
    {
        if (evaluation.Status.ToLower() == "completed" || 
            evaluation.Status.ToLower() == "completed_without_promotion")
        {
            return evaluation.Reviews
                .Where(r => r.SubmittedAt.HasValue)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefault()?.SubmittedAt;
        }
        return null;
    }

    private decimal? GetReviewScore(Evaluation evaluation, ReviewerRole role)
    {
        var review = evaluation.Reviews
            .FirstOrDefault(r => r.ReviewerRole == role && 
                              (r.Status.ToLower() == "completed" || r.Status.ToLower() == "approved"));
        
        if (review == null) return null;

        // For RM role, calculate average of item scores
        if (role == ReviewerRole.RM && review.ReviewItems.Any())
        {
            return review.ReviewItems.Average(ri => ri.RatingValue);
        }

        // For other roles, return overall score
        return review.ReviewItems.Any() ? review.ReviewItems.Average(ri => ri.RatingValue) : null;
    }

    private decimal? GetPeerScore(Evaluation evaluation, int peerIndex)
    {
        var peerReviews = evaluation.Reviews
            .Where(r => r.ReviewerRole == ReviewerRole.Peer && 
                       (r.Status.ToLower() == "completed" || r.Status.ToLower() == "approved"))
            .OrderBy(r => r.ReviewId)
            .ToList();

        if (peerIndex >= peerReviews.Count) return null;

        var review = peerReviews[peerIndex];
        return review.ReviewItems.Any() ? review.ReviewItems.Average(ri => ri.RatingValue) : null;
    }
}
