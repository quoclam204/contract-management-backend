using System.IO;
using System.Net;
using System.Text.Json;
using ContractManagement.Api.Middlewares;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContractManagement.UnitTests.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private readonly NullLogger<ExceptionHandlingMiddleware> _logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ShouldReturn400BadRequest()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(new[]
            {
                new ValidationFailure("TaxCode", "Mã số thuế hoặc Tên đối tác đã tồn tại trong hệ thống.")
            }),
            _logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();

        var json = JsonDocument.Parse(responseText);
        json.RootElement.GetProperty("message").GetString()
            .Should().Be("Mã số thuế hoặc Tên đối tác đã tồn tại trong hệ thống.");
        json.RootElement.GetProperty("status").GetInt32()
            .Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WhenDbUpdateExceptionWithUniqueConstraint_ShouldReturn400BadRequest()
    {
        // Arrange
        var innerException = new Exception("Violation of UNIQUE KEY constraint 'UX_PARTNERS_TaxCode'. Cannot insert duplicate key in object 'dbo.PARTNERS'.");
        var dbUpdateException = new DbUpdateException("An error occurred while saving the entity changes.", innerException);

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw dbUpdateException,
            _logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();

        var json = JsonDocument.Parse(responseText);
        json.RootElement.GetProperty("message").GetString()
            .Should().Be("Mã số thuế hoặc Tên đối tác đã tồn tại trong hệ thống.");
        json.RootElement.GetProperty("status").GetInt32()
            .Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ShouldReturn400BadRequest()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("Dữ liệu đầu vào không hợp lệ."),
            _logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();

        var json = JsonDocument.Parse(responseText);
        json.RootElement.GetProperty("message").GetString()
            .Should().Be("Dữ liệu đầu vào không hợp lệ.");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ShouldReturn500InternalServerError()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidTimeZoneException("Server crash"),
            _logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();

        var json = JsonDocument.Parse(responseText);
        json.RootElement.GetProperty("message").GetString()
            .Should().Be("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
    }
}
