using DigitalPlatform.Application.Common;
using System.Text.Json;

namespace DigitalPlatform.API.Middleware;

public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private const string ConfigKey  = "ApiKey";

    private readonly RequestDelegate _next;
    private readonly string          _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next   = next;
        _apiKey = config[ConfigKey]
            ?? throw new InvalidOperationException(
                $"La clave '{ConfigKey}' no está definida en la configuración.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Excluir Swagger en cualquier entorno donde esté habilitado
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var receivedKey)
            || !string.Equals(receivedKey, _apiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(
                ApiResponse<object>.Fail("API Key inválida o ausente."),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await context.Response.WriteAsync(body);
            return;
        }

        await _next(context);
    }
}
