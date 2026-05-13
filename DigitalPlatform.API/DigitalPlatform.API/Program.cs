using DigitalPlatform.Application.Interfaces;
using DigitalPlatform.Application.Interfaces.Parsers;
using DigitalPlatform.Infrastructure.Parsers;
using DigitalPlatform.Infrastructure.Persistence;
using DigitalPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Parsers
builder.Services.AddScoped<IGR55Parser, GR55Parser>();
builder.Services.AddScoped<IHorasParser, HorasParser>();
builder.Services.AddScoped<IPlaneacionParser, PlaneacionParser>();
builder.Services.AddScoped<ITipoCambioParser, TipoCambioParser>();
builder.Services.AddScoped<IMaestroReferenciasParser, MaestroReferenciasParser>();

// Services
builder.Services.AddScoped<IConsolidacionService, ConsolidacionService>();
// builder.Services.AddScoped<IProyectoService, ProyectoService>();           // Task 16

// Permitir archivos grandes (100 MB) en uploads multipart
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 104_857_600; // 100 MB
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 104_857_600; // 100 MB
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Plataforma Digital API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Plataforma Digital API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
