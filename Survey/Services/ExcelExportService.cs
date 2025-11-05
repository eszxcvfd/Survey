using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Survey.DTOs;
using QuestPDF.Helpers; // Add this using directive

namespace Survey.Services
{
    public class ExcelExportService : IDataExportService
    {
        private readonly ILogger<ExcelExportService> _logger;

        public ExcelExportService(ILogger<ExcelExportService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> ExportToExcelAsync(List<RawResponseViewModel> data, string surveyTitle)
        {
            _logger.LogInformation("Exporting {Count} responses to Excel", data.Count);

            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Responses");

                // Style header
                var headerStyle = workbook.Style;
                headerStyle.Font.Bold = true;
                headerStyle.Fill.BackgroundColor = XLColor.FromHtml("#6750A4");
                headerStyle.Font.FontColor = XLColor.White;
                headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add title
                worksheet.Cell(1, 1).Value = $"Survey: {surveyTitle}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 4).Merge();

                // Add export date
                worksheet.Cell(2, 1).Value = $"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                worksheet.Range(2, 1, 2, 4).Merge();

                // ✅ FIX: Get ALL unique questions across ALL responses
                var allQuestions = data
                    .SelectMany(r => r.Answers.Keys)
                    .Distinct()
                    .OrderBy(q => q)
                    .ToList();

                _logger.LogInformation("Found {Count} unique questions across responses", allQuestions.Count);

                // Add headers (row 4)
                int currentRow = 4;
                int currentCol = 1;

                worksheet.Cell(currentRow, currentCol++).Value = "Response ID";
                worksheet.Cell(currentRow, currentCol++).Value = "Submitted At";
                worksheet.Cell(currentRow, currentCol++).Value = "Channel";

                // ✅ Add ALL questions as columns
                foreach (var question in allQuestions)
                {
                    worksheet.Cell(currentRow, currentCol++).Value = question;
                }

                // Apply header style
                var headerRange = worksheet.Range(currentRow, 1, currentRow, currentCol - 1);
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#6750A4");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add data rows
                currentRow++;
                foreach (var response in data)
                {
                    currentCol = 1;
                    worksheet.Cell(currentRow, currentCol++).Value = response.ResponseId.ToString();
                    worksheet.Cell(currentRow, currentCol++).Value = response.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(currentRow, currentCol++).Value = response.ChannelType ?? "Unknown";

                    // ✅ Fill answers for ALL questions (even if empty)
                    foreach (var question in allQuestions)
                    {
                        var answer = response.Answers.ContainsKey(question) 
                            ? response.Answers[question] 
                            : ""; // Empty if no answer
                        worksheet.Cell(currentRow, currentCol++).Value = answer;
                    }

                    currentRow++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Add borders
                var dataRange = worksheet.Range(4, 1, currentRow - 1, currentCol - 1);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                
                _logger.LogInformation("Excel file generated successfully with {Rows} rows and {Cols} columns", 
                    currentRow - 4, currentCol - 1);
                
                return stream.ToArray();
            });
        }

        public async Task<byte[]> ExportToPdfAsync(List<RawResponseViewModel> data, string surveyTitle)
        {
            _logger.LogInformation("Exporting {Count} responses to PDF", data.Count);

            return await Task.Run(() =>
            {
                using var stream = new MemoryStream();
                
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        // Header
                        page.Header().Column(column =>
                        {
                            column.Item().Text($"Survey: {surveyTitle}")
                                .FontSize(18)
                                .Bold()
                                .FontColor("#6750A4");

                            column.Item().Text($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                                .FontSize(10)
                                .FontColor("#666666");

                            column.Item().PaddingVertical(5);
                        });

                        // Content
                        page.Content().Column(column =>
                        {
                            // ✅ Get ALL unique questions
                            var allQuestions = data
                                .SelectMany(r => r.Answers.Keys)
                                .Distinct()
                                .OrderBy(q => q)
                                .Take(8) // Limit to 8 questions for PDF width
                                .ToList();

                            _logger.LogInformation("PDF: Found {Count} unique questions", allQuestions.Count);

                            // Create table
                            column.Item().Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80); // Response ID
                                    columns.ConstantColumn(100); // Submitted At
                                    columns.ConstantColumn(80); // Channel

                                    foreach (var _ in allQuestions)
                                    {
                                        columns.RelativeColumn(); // Questions
                                    }
                                });

                                // Header row
                                table.Header(header =>
                                {
                                    header.Cell().Background("#6750A4")
                                        .Padding(5)
                                        .Text("Response ID")
                                        .FontColor("#FFFFFF")
                                        .Bold();

                                    header.Cell().Background("#6750A4")
                                        .Padding(5)
                                        .Text("Submitted At")
                                        .FontColor("#FFFFFF")
                                        .Bold();

                                    header.Cell().Background("#6750A4")
                                        .Padding(5)
                                        .Text("Channel")
                                        .FontColor("#FFFFFF")
                                        .Bold();

                                    foreach (var question in allQuestions)
                                    {
                                        header.Cell().Background("#6750A4")
                                            .Padding(5)
                                            .Text(TruncateText(question, 30))
                                            .FontColor("#FFFFFF")
                                            .Bold();
                                    }
                                });

                                // Data rows
                                foreach (var response in data.Take(50)) // Limit to 50 responses for PDF
                                {
                                    table.Cell().Border(1).Padding(3)
                                        .Text(response.ResponseId.ToString().Substring(0, 8));

                                    table.Cell().Border(1).Padding(3)
                                        .Text(response.SubmittedAt.ToString("MM/dd HH:mm"));

                                    table.Cell().Border(1).Padding(3)
                                        .Text(response.ChannelType ?? "N/A");

                                    // ✅ Add answers for ALL questions
                                    foreach (var question in allQuestions)
                                    {
                                        var answer = response.Answers.ContainsKey(question)
                                            ? response.Answers[question]
                                            : "";

                                        table.Cell().Border(1).Padding(3)
                                            .Text(TruncateText(answer, 50));
                                    }
                                }
                            });

                            // Add note if data was truncated
                            if (data.Count > 50)
                            {
                                column.Item().PaddingTop(10).Text($"Note: Showing first 50 of {data.Count} responses")
                                    .FontSize(9)
                                    .Italic()
                                    .FontColor("#666666");
                            }
                            
                            if (allQuestions.Count < data.SelectMany(r => r.Answers.Keys).Distinct().Count())
                            {
                                column.Item().Text($"Note: Showing first 8 questions due to PDF width limitation")
                                    .FontSize(9)
                                    .Italic()
                                    .FontColor("#666666");
                            }
                        });

                        // Footer
                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                });

                document.GeneratePdf(stream);
                
                _logger.LogInformation("PDF file generated successfully");
                
                return stream.ToArray();
            });
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}