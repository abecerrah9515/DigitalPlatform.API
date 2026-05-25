using DigitalPlatform.Application.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DigitalPlatform.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate               _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
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
            _logger.LogError(ex, "Excepción no manejada en {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var (statusCode, mensaje) = ex switch
            {
                FileNotFoundException   => (StatusCodes.Status500InternalServerError, "Archivo no encontrado."),
                DbUpdateException       => (StatusCodes.Status500InternalServerError, "Error al guardar en base de datos."),
                _                       => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Contacta al administrador."),
            };

            context.Response.StatusCode  = statusCode;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(
                ApiResponse<object>.Fail(mensaje),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await context.Response.WriteAsync(body);
        }
    }
}
