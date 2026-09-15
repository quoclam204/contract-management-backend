using System.Text;
using ContractManagement.Api.Services;
using ContractManagement.Application;
using ContractManagement.Application.AI.Interfaces;
using ContractManagement.Application.AI.Services;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Application.Contract.Services;
using ContractManagement.Infrastructure.Email;
using ContractManagement.Application.Dashboard.Interfaces;
using ContractManagement.Application.Dashboard.Services;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Application.Identity.Services;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Application.Notification.Services;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Application.Workflow.Services;
using ContractManagement.Domain.Identity.Enums;
using ContractManagement.Infrastructure.AI;
using ContractManagement.Infrastructure;
using ContractManagement.Infrastructure.Messaging;
using ContractManagement.Infrastructure.Persistence;
using ContractManagement.Infrastructure.Security;
using ContractManagement.Infrastructure.Storage;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

// CORS for Frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ContractManagementDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
});

// DbContext Interfaces
builder.Services.AddScoped<IWorkflowDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
builder.Services.AddScoped<IAiDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
// Notification Module Services
builder.Services.AddScoped<IContractManagementDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
builder.Services.AddScoped<ContractManagement.Application.Contracts.Interfaces.IContractDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
builder.Services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
builder.Services.AddScoped<IPartnerDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
builder.Services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
builder.Services.AddScoped<IAttachmentDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());

// Notification Module Services
builder.Services.AddScoped<INotificationService, NotificationService>();

// RabbitMQ Messaging
builder.Services.AddRabbitMqMessaging(builder.Configuration);

// Infrastructure Services (Storage, etc.)
builder.Services.AddInfrastructureServices();

// Storage Services
var storagePath = builder.Configuration["Storage:LocalPath"] ?? "./storage";
builder.Services.AddScoped<IStorageProvider>(_ => new LocalStorageProvider(storagePath));
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

// Application Services (MediatR, FluentValidation, ValidationBehavior)
builder.Services.AddApplicationServices();

// Security & CurrentUser
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Identity & Department Services
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Contract Module Services
builder.Services.AddScoped<IContractTypeService, ContractTypeService>();
builder.Services.AddScoped<IContractTemplateVersionService, ContractTemplateVersionService>();

// Dashboard Module Services
builder.Services.AddScoped<IDashboardService, DashboardService>();
// Legacy Contract Module Services
builder.Services.AddScoped<ContractManagement.Application.Contracts.Interfaces.IContractService, ContractManagement.Application.Contracts.Services.ContractService>();
builder.Services.AddScoped<ContractManagement.Application.Contracts.Interfaces.IContractTypeService, ContractManagement.Application.Contracts.Services.ContractTypeService>();
builder.Services.AddScoped<ContractManagement.Application.Contracts.Interfaces.IContractTemplateVersionService, ContractManagement.Application.Contracts.Services.ContractTemplateVersionService>();

// Email (FR-08) — minimal SMTP abstraction
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Contract Expiry (FR-08)
builder.Services.AddScoped<ContractManagement.Application.Contracts.Interfaces.IContractExpiryJobService, ContractManagement.Application.Contracts.Services.ContractExpiryJobService>();

// AI Module Services
builder.Services.AddScoped<IAIContractAssistantService, MockAIContractAssistantService>();
builder.Services.AddScoped<IAIAnalysisJobService, AIAnalysisJobService>();

// Hangfire — AI analysis background jobs (SqlServer storage reusing DefaultConnection)
// Application layer remains free of Hangfire types; Hangfire is API/Infrastructure concern only.
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true
        }));
    builder.Services.AddHangfireServer();

    // FR-08: retry on DB/email transient failures, prevent overlapping executions
    GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail });
    GlobalJobFilters.Filters.Add(new DisableConcurrentExecutionAttribute(600));
}

// Workflow Module Services (Reference Implementation)
builder.Services.AddScoped<IWorkflowConditionEvaluator, WorkflowConditionEvaluator>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ISignatureProvider, ContractManagement.Application.Workflow.Services.SignatureProviders.MockSignatureProvider>();
builder.Services.AddScoped<ISignatureProvider, ContractManagement.Application.Workflow.Services.SignatureProviders.OtpSignatureProvider>();
builder.Services.AddScoped<ISignatureService, SignatureService>();

// JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "ContractManagementSuperSecretKey2026!@#$%^&*()_+";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ContractManagement";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ContractManagementApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// RBAC Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy("RequireManager", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Manager.ToString()));
    options.AddPolicy("RequireApprover", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Approver.ToString()));
});

var app = builder.Build();

// Hangfire recurring job — FR-08 contract expiry (daily)
try
{
    using var scope = app.Services.CreateScope();
    var recurring = scope.ServiceProvider.GetService<IRecurringJobManager>();
    recurring?.AddOrUpdate<ContractManagement.Application.Contracts.Interfaces.IContractExpiryJobService>(
        "contract-expiry-notification-job",
        service => service.ProcessContractExpirationsAsync(CancellationToken.None),
        Cron.Daily);
}
catch (Exception ex)
{
    var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("Hangfire");
    logger?.LogWarning(ex, "Failed to schedule contract-expiry-notification-job");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Contract Management API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.Run();
