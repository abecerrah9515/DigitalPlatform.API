using Microsoft.EntityFrameworkCore;
using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Cartera;
using DigitalPlatform.Application.Interfaces;
using DigitalPlatform.Infrastructure.Persistence;

namespace DigitalPlatform.Infrastructure.Services;

public class DepartamentoFinanzasService : IDepartamentoFinanzasService
{
    private readonly ApplicationDbContext _db;

    public DepartamentoFinanzasService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<DepartamentoFinanzasDto>>> ObtenerTodosAsync()
    {
        var departamentos = await _db.DepartamentosFinanzas
            .OrderBy(d => d.Nombre)
            .Select(d => new DepartamentoFinanzasDto
            {
                Id = d.Id,
                Nombre = d.Nombre,
            })
            .ToListAsync();

        return ApiResponse<List<DepartamentoFinanzasDto>>.Ok(departamentos);
    }
}
