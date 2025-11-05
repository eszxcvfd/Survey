using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Repositories;
using Survey.Services;

namespace Survey.Controllers
{
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IDataExportService _exportService;
        private readonly ISurveyRepository _surveyRepository;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            IDataExportService exportService,
            ISurveyRepository surveyRepository,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _exportService = exportService;
            _surveyRepository = surveyRepository;
            _logger = logger;
        }

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        private Guid? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Hiển thị trang báo cáo với thống kê và biểu đồ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(Guid surveyId, [FromQuery] FilterViewModel? filters)
        {
            _logger.LogInformation("=== Report Index called for survey {SurveyId} ===", surveyId);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                filters ??= new FilterViewModel();

                var report = await _reportService.GetAggregatedReportAsync(surveyId, filters, currentUserId.Value);
                
                return View(report);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "You don't have permission to view this report";
                return RedirectToAction("Index", "Survey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report for survey {SurveyId}", surveyId);
                TempData["ErrorMessage"] = "An error occurred while loading the report";
                return RedirectToAction("Index", "Survey");
            }
        }

        /// <summary>
        /// Xuất dữ liệu ra Excel
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportExcel(Guid surveyId, [FromQuery] FilterViewModel? filters)
        {
            _logger.LogInformation("=== Export Excel called for survey {SurveyId} ===", surveyId);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                filters ??= new FilterViewModel();

                // Get raw data
                var rawData = await _reportService.GetRawResponsesAsync(surveyId, filters, currentUserId.Value);

                if (!rawData.Any())
                {
                    TempData["ErrorMessage"] = "No data to export";
                    return RedirectToAction("Index", new { surveyId });
                }

                // Get survey title
                var survey = await _surveyRepository.GetByIdAsync(surveyId);
                var surveyTitle = survey?.Title ?? "Survey";

                // Export to Excel
                var fileBytes = await _exportService.ExportToExcelAsync(rawData, surveyTitle);

                // Generate filename
                var filename = $"{surveyTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filename
                );
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "You don't have permission to export this data";
                return RedirectToAction("Index", "Survey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel for survey {SurveyId}", surveyId);
                TempData["ErrorMessage"] = "An error occurred while exporting the data";
                return RedirectToAction("Index", new { surveyId });
            }
        }

        /// <summary>
        /// Xuất dữ liệu ra PDF
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportPdf(Guid surveyId, [FromQuery] FilterViewModel? filters)
        {
            _logger.LogInformation("=== Export PDF called for survey {SurveyId} ===", surveyId);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                filters ??= new FilterViewModel();

                // Get raw data
                var rawData = await _reportService.GetRawResponsesAsync(surveyId, filters, currentUserId.Value);

                if (!rawData.Any())
                {
                    TempData["ErrorMessage"] = "No data to export";
                    return RedirectToAction("Index", new { surveyId });
                }

                // Get survey title
                var survey = await _surveyRepository.GetByIdAsync(surveyId);
                var surveyTitle = survey?.Title ?? "Survey";

                // Export to PDF
                var fileBytes = await _exportService.ExportToPdfAsync(rawData, surveyTitle);

                // Generate filename
                var filename = $"{surveyTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(fileBytes, "application/pdf", filename);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "You don't have permission to export this data";
                return RedirectToAction("Index", "Survey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PDF for survey {SurveyId}", surveyId);
                TempData["ErrorMessage"] = "An error occurred while exporting the data";
                return RedirectToAction("Index", new { surveyId });
            }
        }
    }
}