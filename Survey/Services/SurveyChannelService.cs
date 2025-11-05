using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;

namespace Survey.Services
{
    public class SurveyChannelService : ISurveyChannelService
    {
        private readonly ISurveyChannelRepository _channelRepository;
        private readonly ISurveyRepository _surveyRepository;
        private readonly ISurveyCollaboratorRepository _collaboratorRepository;
        private readonly ISlugGenerator _slugGenerator;
        private readonly IQrCodeService _qrCodeService;
        private readonly IEmailService _emailService; // ← ĐÃ CÓ SẴN
        private readonly ILogger<SurveyChannelService> _logger;

        public SurveyChannelService(
            ISurveyChannelRepository channelRepository,
            ISurveyRepository surveyRepository,
            ISurveyCollaboratorRepository collaboratorRepository,
            ISlugGenerator slugGenerator,
            IQrCodeService qrCodeService,
            IEmailService emailService, // ← INJECT
            ILogger<SurveyChannelService> logger)
        {
            _channelRepository = channelRepository;
            _surveyRepository = surveyRepository;
            _collaboratorRepository = collaboratorRepository;
            _slugGenerator = slugGenerator;
            _qrCodeService = qrCodeService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ManageChannelsViewModel?> GetChannelsForSurveyAsync(Guid surveyId, Guid currentUserId)
        {
            _logger.LogInformation("Getting channels for survey {SurveyId}", surveyId);

            try
            {
                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(surveyId, currentUserId);
                if (collaboration == null)
                {
                    _logger.LogWarning("User {UserId} does not have access to survey {SurveyId}", currentUserId, surveyId);
                    return null;
                }

                var survey = await _surveyRepository.GetByIdAsync(surveyId);
                if (survey == null)
                {
                    return null;
                }

                var channels = await _channelRepository.GetBySurveyIdAsync(surveyId);

                var channelViewModels = new List<ChannelViewModel>();
                foreach (var channel in channels)
                {
                    var responseCount = await _channelRepository.GetResponseCountByChannelAsync(channel.ChannelId);
                    
                    channelViewModels.Add(new ChannelViewModel
                    {
                        ChannelId = channel.ChannelId,
                        ChannelType = channel.ChannelType,
                        PublicUrlSlug = channel.PublicUrlSlug,
                        FullUrl = channel.FullUrl,
                        QrImagePath = channel.QrImagePath,
                        EmailSubject = channel.EmailSubject,
                        SentAtUtc = channel.SentAtUtc,
                        IsActive = channel.IsActive,
                        CreatedAtUtc = channel.CreatedAtUtc,
                        ResponseCount = responseCount
                    });
                }

                return new ManageChannelsViewModel
                {
                    SurveyId = survey.SurveyId,
                    SurveyTitle = survey.Title,
                    SurveyStatus = survey.Status,
                    ExistingChannels = channelViewModels,
                    CanEdit = collaboration.Role == "Owner" || collaboration.Role == "Editor"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting channels for survey {SurveyId}", surveyId);
                return null;
            }
        }

        public async Task<ServiceResult<ChannelViewModel>> CreateChannelAsync(CreateChannelDto model, Guid currentUserId, string baseUrl)
        {
            _logger.LogInformation("Creating channel for survey {SurveyId}, type: {ChannelType}", model.SurveyId, model.ChannelType);

            try
            {
                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(model.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult<ChannelViewModel>.FailureResult("You don't have permission to create channels for this survey");
                }

                // Get survey
                var survey = await _surveyRepository.GetByIdAsync(model.SurveyId);
                if (survey == null)
                {
                    return ServiceResult<ChannelViewModel>.FailureResult("Survey not found");
                }

                // Generate unique slug
                var slug = await _slugGenerator.GenerateUniqueSlugAsync();
                var fullUrl = $"{baseUrl}/take/{slug}";

                // Generate QR code
                var qrImagePath = await _qrCodeService.GenerateAndSaveAsync(fullUrl);

                // Create channel entity
                var channel = new SurveyChannel
                {
                    ChannelId = Guid.NewGuid(),
                    SurveyId = model.SurveyId,
                    ChannelType = model.ChannelType,
                    PublicUrlSlug = slug,
                    FullUrl = fullUrl,
                    QrImagePath = qrImagePath,
                    EmailSubject = model.EmailSubject,
                    EmailBody = model.EmailBody,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _channelRepository.AddAsync(channel);

                // *** MỚI: Gửi email nếu ChannelType là "Email" ***
                if (model.ChannelType == "Email" && !string.IsNullOrEmpty(model.RecipientEmails))
                {
                    await SendSurveyInvitationEmailsAsync(model, survey, fullUrl);
                    channel.SentAtUtc = DateTime.UtcNow;
                    await _channelRepository.UpdateAsync(channel);
                }

                _logger.LogInformation("Channel created successfully: {ChannelId}", channel.ChannelId);

                var viewModel = new ChannelViewModel
                {
                    ChannelId = channel.ChannelId,
                    ChannelType = channel.ChannelType,
                    PublicUrlSlug = channel.PublicUrlSlug,
                    FullUrl = channel.FullUrl,
                    QrImagePath = channel.QrImagePath,
                    EmailSubject = channel.EmailSubject,
                    SentAtUtc = channel.SentAtUtc,
                    IsActive = channel.IsActive,
                    CreatedAtUtc = channel.CreatedAtUtc,
                    ResponseCount = 0
                };

                return ServiceResult<ChannelViewModel>.SuccessResult(viewModel, "Distribution channel created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating channel for survey {SurveyId}", model.SurveyId);
                return ServiceResult<ChannelViewModel>.FailureResult($"Error creating channel: {ex.Message}");
            }
        }

        // *** MỚI: Hàm gửi email mời ***
        private async Task SendSurveyInvitationEmailsAsync(CreateChannelDto model, Models.Survey survey, string surveyUrl)
        {
            try
            {
                // Parse danh sách email (cách nhau bởi dấu phẩy, chấm phẩy, hoặc xuống dòng)
                var emails = model.RecipientEmails?
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList() ?? new List<string>();

                if (!emails.Any())
                {
                    _logger.LogWarning("No recipient emails provided");
                    return;
                }

                // Tạo nội dung email
                var subject = model.EmailSubject ?? $"You're invited: {survey.Title}";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #6750A4;'>You're invited to participate in a survey!</h2>
                            <p><strong>{survey.Title}</strong></p>
                            {(!string.IsNullOrEmpty(survey.Description) ? $"<p>{survey.Description}</p>" : "")}
                            
                            {(!string.IsNullOrEmpty(model.EmailBody) ? $"<p>{model.EmailBody}</p>" : "")}
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{surveyUrl}' 
                                   style='display: inline-block; padding: 12px 24px; background-color: #6750A4; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>
                                    Take Survey
                                </a>
                            </div>
                            
                            <p style='font-size: 12px; color: #666; margin-top: 20px;'>
                                Or copy and paste this link into your browser:<br/>
                                <a href='{surveyUrl}' style='color: #6750A4;'>{surveyUrl}</a>
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                // Gửi email cho từng người nhận
                int successCount = 0;
                foreach (var email in emails)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(email, subject, body);
                        successCount++;
                        _logger.LogInformation("Survey invitation sent to {Email}", email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send invitation to {Email}", email);
                    }
                }

                _logger.LogInformation("Sent {SuccessCount}/{TotalCount} survey invitations", successCount, emails.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending survey invitation emails");
            }
        }

        public async Task<ServiceResult> DeleteChannelAsync(Guid channelId, Guid currentUserId)
        {
            _logger.LogInformation("Deleting channel {ChannelId}", channelId);

            try
            {
                var channel = await _channelRepository.GetByIdAsync(channelId);
                if (channel == null)
                {
                    return ServiceResult.FailureResult("Channel not found");
                }

                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(channel.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult.FailureResult("You don't have permission to delete this channel");
                }

                await _channelRepository.DeleteAsync(channelId);

                _logger.LogInformation("Channel deleted successfully: {ChannelId}", channelId);
                return ServiceResult.SuccessResult("Channel deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting channel {ChannelId}", channelId);
                return ServiceResult.FailureResult($"Error deleting channel: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ToggleChannelStatusAsync(Guid channelId, Guid currentUserId)
        {
            _logger.LogInformation("Toggling channel status {ChannelId}", channelId);

            try
            {
                var channel = await _channelRepository.GetByIdAsync(channelId);
                if (channel == null)
                {
                    return ServiceResult.FailureResult("Channel not found");
                }

                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(channel.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult.FailureResult("You don't have permission to modify this channel");
                }

                channel.IsActive = !channel.IsActive;
                await _channelRepository.UpdateAsync(channel);

                var status = channel.IsActive ? "activated" : "deactivated";
                _logger.LogInformation("Channel {ChannelId} {Status}", channelId, status);
                
                return ServiceResult.SuccessResult($"Channel {status} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling channel status {ChannelId}", channelId);
                return ServiceResult.FailureResult($"Error updating channel status: {ex.Message}");
            }
        }
    }
}