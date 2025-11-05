using Microsoft.EntityFrameworkCore;
using Survey.Models;
using Survey.Repositories;
using Survey.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký DbContext
builder.Services.AddDbContext<SurveyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
builder.Services.AddScoped<ISurveyCollaboratorRepository, SurveyCollaboratorRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionOptionRepository, QuestionOptionRepository>();
builder.Services.AddScoped<IBranchLogicRepository, BranchLogicRepository>();

// *** PHẦN 2: Survey Taker Repositories ***
builder.Services.AddScoped<ISurveyResponseRepository, SurveyResponseRepository>();
builder.Services.AddScoped<IResponseAnswerRepository, ResponseAnswerRepository>();

// Đăng ký Security Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenGenerator, TokenGenerator>();

// Đăng ký Service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISurveyCollaboratorService, SurveyCollaboratorService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<ISurveyDesignerService, SurveyDesignerService>();
builder.Services.AddScoped<IBranchLogicService, BranchLogicService>();

// Channel Management
builder.Services.AddScoped<ISurveyChannelRepository, SurveyChannelRepository>();
builder.Services.AddScoped<ISurveyChannelService, SurveyChannelService>();
builder.Services.AddScoped<ISlugGenerator, SlugGenerator>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();

// *** PHẦN 2: Survey Taker Services (Logic Engine & Survey Taking) ***
builder.Services.AddScoped<ILogicEngineService, LogicEngineService>();
builder.Services.AddScoped<ISurveyTakerService, SurveyTakerService>();

// Register Report Services
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDataExportService, ExcelExportService>();

// Thêm Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Thêm HttpClient cho RecaptchaService
builder.Services.AddHttpClient<ICaptchaService, RecaptchaService>();
builder.Services.AddScoped<ICaptchaService, RecaptchaService>();

// Configure HSTS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
