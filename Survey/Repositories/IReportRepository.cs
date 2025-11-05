using Survey.DTOs;

namespace Survey.Repositories
{
    /// <summary>
    /// Repository chuyên biệt cho việc tổng hợp và phân tích dữ liệu
    /// Không dùng CRUD pattern, mà dùng truy vấn tối ưu (Dapper hoặc Raw SQL)
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>
        /// Lấy dữ liệu tổng hợp cho các câu hỏi
        /// </summary>
        Task<List<AggregatedResultDto>> GetAggregatedStatsAsync(Guid surveyId, FilterViewModel filters);

        /// <summary>
        /// Lấy dữ liệu thô (flat) cho việc xuất file
        /// </summary>
        Task<List<RawResponseViewModel>> GetRawResponsesAsync(Guid surveyId, FilterViewModel filters);

        /// <summary>
        /// Đếm số lượng response
        /// </summary>
        Task<int> GetTotalResponsesAsync(Guid surveyId, FilterViewModel filters);

        /// <summary>
        /// Đếm số response đã hoàn thành
        /// </summary>
        Task<int> GetCompletedResponsesAsync(Guid surveyId, FilterViewModel filters);
    }
}