using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Contracts.Services;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Application.Workflow.Services;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

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

builder.Services.AddScoped<IWorkflowDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
builder.Services.AddScoped<IContractDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(WorkflowService).Assembly);
});

// Workflow Module Services
builder.Services.AddScoped<IWorkflowConditionEvaluator, WorkflowConditionEvaluator>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

// Contract Module Services
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IContractTypeService, ContractTypeService>();
builder.Services.AddScoped<IContractTemplateVersionService, ContractTemplateVersionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();