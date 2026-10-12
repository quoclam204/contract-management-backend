using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, responseBody) = exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            DbUpdateException dbUpdateEx => HandleDbUpdateException(dbUpdateEx),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                (object)new
                {
                    message = argEx.Message,
                    error = argEx.Message,
                    status = (int)HttpStatusCode.BadRequest
                }
            ),
            InvalidOperationException invalidOpEx => (
                HttpStatusCode.BadRequest,
                (object)new
                {
                    message = invalidOpEx.Message,
                    error = invalidOpEx.Message,
                    status = (int)HttpStatusCode.BadRequest
                }
            ),
            KeyNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                (object)new
                {
                    message = notFoundEx.Message,
                    error = notFoundEx.Message,
                    status = (int)HttpStatusCode.NotFound
                }
            ),
            _ => HandleGenericException(exception)
        };

        response.StatusCode = (int)statusCode;
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        await response.WriteAsync(JsonSerializer.Serialize(responseBody, jsonOptions));
    }

    private (HttpStatusCode, object) HandleValidationException(ValidationException ex)
    {
        var firstError = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
        var errorsDictionary = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var responseBody = new
        {
            message = firstError,
            error = firstError,
            errors = errorsDictionary,
            status = (int)HttpStatusCode.BadRequest
        };

        return (HttpStatusCode.BadRequest, responseBody);
    }

    private (HttpStatusCode, object) HandleDbUpdateException(DbUpdateException ex)
    {
        _logger.LogWarning(ex, "DbUpdateException occurred during request processing");

        var message = "Đã xảy ra lỗi cập nhật cơ sở dữ liệu.";
        var isUniqueViolation = false;

        // Check SqlException (SQL Server error 2601 or 2627 for unique index / primary key violation)
        if (ex.InnerException is SqlException sqlEx)
        {
            if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
            {
                isUniqueViolation = true;
            }
        }
        else if (ex.InnerException?.Message != null)
        {
            var msg = ex.InnerException.Message.ToLowerInvariant();
            if (msg.Contains("duplicate key") || msg.Contains("unique index") || msg.Contains("unique constraint") || msg.Contains("ux_") || msg.Contains("uq_"))
            {
                isUniqueViolation = true;
            }
        }

        if (isUniqueViolation)
        {
            var fullText = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
            if (fullText.Contains("taxcode") || fullText.Contains("partner") || fullText.Contains("ux_partners"))
            {
                message = "Mã số thuế hoặc Tên đối tác đã tồn tại trong hệ thống.";
            }
            else
            {
                message = "Dữ liệu đã tồn tại trong hệ thống.";
            }

            return (
                HttpStatusCode.BadRequest,
                new
                {
                    message,
                    error = message,
                    status = (int)HttpStatusCode.BadRequest
                }
            );
        }

        // Check foreign key constraint violation (SQL Server error 547)
        if (ex.InnerException is SqlException fkEx && fkEx.Number == 547)
        {
            message = "Dữ liệu liên quan không hợp lệ hoặc đang được tham chiếu bởi bản ghi khác.";
            return (
                HttpStatusCode.BadRequest,
                new
                {
                    message,
                    error = message,
                    status = (int)HttpStatusCode.BadRequest
                }
            );
        }

        return (
            HttpStatusCode.BadRequest,
            new
            {
                message = "Không thể lưu thay đổi vào cơ sở dữ liệu. Vui lòng kiểm tra lại thông tin.",
                error = "Không thể lưu thay đổi vào cơ sở dữ liệu. Vui lòng kiểm tra lại thông tin.",
                status = (int)HttpStatusCode.BadRequest
            }
        );
    }

    private (HttpStatusCode, object) HandleGenericException(Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);

        return (
            HttpStatusCode.InternalServerError,
            new
            {
                message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.",
                error = "InternalServerError",
                status = (int)HttpStatusCode.InternalServerError
            }
        );
    }
}
