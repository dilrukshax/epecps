using Epecps.Application.DTOs.Reports;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for generating HR reports
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Get evaluation report data with filters
    /// </summary>
    Task<List<EvaluationReportDataDto>> GetEvaluationReportDataAsync(
        EvaluationReportFilterDto filter, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate Excel report for evaluations
    /// </summary>
    Task<byte[]> GenerateEvaluationExcelReportAsync(
        EvaluationReportFilterDto filter,
        CancellationToken cancellationToken = default);
}
