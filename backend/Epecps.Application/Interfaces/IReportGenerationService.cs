namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for generating evaluation reports
/// </summary>
public interface IReportGenerationService
{
    /// <summary>
    /// Generate comprehensive evaluation report as Excel file
    /// </summary>
    /// <param name="cycleId">Optional filter by evaluation cycle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<(byte[] FileData, string FileName)> GenerateEvaluationReportAsync(
        int? cycleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate promotion summary report as Excel file
    /// </summary>
    /// <param name="cycleId">Optional filter by evaluation cycle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<(byte[] FileData, string FileName)> GeneratePromotionReportAsync(
        int? cycleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate department-wise performance summary report
    /// </summary>
    /// <param name="cycleId">Optional filter by evaluation cycle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<(byte[] FileData, string FileName)> GenerateDepartmentReportAsync(
        int? cycleId = null,
        CancellationToken cancellationToken = default);
}
