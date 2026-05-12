using DigitalPlatform.Application.Interfaces.Parsers;
using DigitalPlatform.Infrastructure.Parsers;
using DigitalPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Parsers
builder.Services.AddScoped<IGR55Parser, GR55Parser>();
builder.Services.AddScoped<IHorasParser, HorasParser>();
builder.Services.AddScoped<IPlaneacionParser, PlaneacionParser>();
builder.Services.AddScoped<ITipoCambioParser, TipoCambioParser>();
// builder.Services.AddScoped<IMaestroReferenciasParser, MaestroReferenciasParser>(); // Task 29 - pendiente

// Services (Juan: implementar en Infrastructure/Services/)
// builder.Services.AddScoped<IConsolidacionService, ConsolidacionService>(); // Task 10
// builder.Services.AddScoped<IProyectoService, ProyectoService>();           // Task 16

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
