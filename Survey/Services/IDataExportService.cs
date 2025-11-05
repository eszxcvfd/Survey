using Survey.DTOs;

namespace Survey.Services
{
    /// <summary>
    /// Service chuyên biệt cho việc xuất dữ liệu sang Excel/PDF
    /// </summary>
    public interface IDataExportService
    {
        /// <summary>
        /// Xuất dữ liệu ra file Excel
        /// </summary>
        Task<byte[]> ExportToExcelAsync(List<RawResponseViewModel> data, string surveyTitle);

        /// <summary>
        /// Xuất dữ liệu ra file PDF
        /// </summary>
        Task<byte[]> ExportToPdfAsync(List<RawResponseViewModel> data, string surveyTitle);
    }
}