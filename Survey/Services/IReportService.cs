using Survey.DTOs;

namespace Survey.Services
{
    public interface IReportService
    {
        /// <summary>
        /// Lấy báo cáo tổng hợp với biểu đồ
        /// </summary>
        Task<ReportViewModel> GetAggregatedReportAsync(Guid surveyId, FilterViewModel filters, Guid currentUserId);

        /// <summary>
        /// Lấy dữ liệu thô cho việc xuất file
        /// </summary>
        Task<List<RawResponseViewModel>> GetRawResponsesAsync(Guid surveyId, FilterViewModel filters, Guid currentUserId);
    }
}