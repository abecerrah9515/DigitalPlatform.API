using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Cartera;
using DigitalPlatform.Application.Interfaces;
using DigitalPlatform.Domain.Entities;
using DigitalPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;

namespace DigitalPlatform.Infrastructure.Services;

public class CarteraService : ICarteraService
{
    private readonly ILogger<CarteraService> _logger;
    private readonly ApplicationDbContext _db;
    private static readonly Random _rng = new();

    public CarteraService(ILogger<CarteraService> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private async Task<int?> UltimaCargaIdAsync(string tipo)
    {
        return await _db.CargasArchivo
            .Where(c => c.Tipo == tipo)
            .OrderByDescending(c => c.FechaCarga)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
    }

    // ════════════════════════════════════════════════════════════════════
    // Mock data de respaldo
    // ════════════════════════════════════════════════════════════════════

    private static List<CarteraFacturaDto> MockFacturas()
    {
        var clientes = new[] { "Cliente A S.A.S.", "Cliente B S.A.", "Cliente C Ltda.", "Cliente D S.A.S.", "Cliente E S.A." };
        var nits = new[] { "900.123.456-7", "800.234.567-8", "700.345.678-9", "600.456.789-0", "500.567.890-1" };
        var estados = new[] { "Pendiente", "Vencida", "Confirmada", "Pagada" };
        var facturas = new List<CarteraFacturaDto>();

        for (int i = 1; i <= 30; i++)
        {
            var idx = _rng.Next(clientes.Length);
            var fechaEmision = DateTime.Today.AddDays(-_rng.Next(30, 180));
            var fechaVenc = fechaEmision.AddDays(_rng.Next(15, 90));
            var estado = estados[_rng.Next(estados.Length)];
            var diasMora = estado == "Vencida" ? _rng.Next(1, 120) : 0;

            facturas.Add(new CarteraFacturaDto
            {
                Id = i,
                Factura = $"FAC-{2026}-{i:D4}",
                Cliente = clientes[idx].Split(' ')[0],
                RazonSocial = clientes[idx],
                Nit = nits[idx],
                FechaEmision = fechaEmision,
                FechaVencimiento = fechaVenc,
                Monto = Math.Round((decimal)(_rng.NextDouble() * 50000000 + 1000000), 2),
                Estado = estado,
                DiasMora = diasMora,
            });
        }
        return facturas;
    }

    private static List<CarteraFacturaDto> MockHistoricoFacturas()
    {
        return
        [
            new() { Id = 1, Factura = "F001-2026", Cliente = "Bancolombia S.A.", RazonSocial = "Bancolombia S.A.", Nit = "", FechaEmision = new(2026, 1, 15), FechaVencimiento = new(2026, 3, 15), Monto = 120000000, Estado = "Pendiente" },
            new() { Id = 2, Factura = "F002-2026", Cliente = "Grupo Éxito S.A.S.", RazonSocial = "Grupo Éxito S.A.S.", Nit = "", FechaEmision = new(2026, 2, 1), FechaVencimiento = new(2026, 4, 1), Monto = 85000000, Estado = "Pendiente" },
            new() { Id = 3, Factura = "F003-2026", Cliente = "Davivienda S.A.", RazonSocial = "Davivienda S.A.", Nit = "", FechaEmision = new(2026, 2, 20), FechaVencimiento = new(2026, 5, 20), Monto = 200000000, Estado = "Vencida" },
            new() { Id = 4, Factura = "F004-2026", Cliente = "Claro Colombia S.A.", RazonSocial = "Claro Colombia S.A.", Nit = "", FechaEmision = new(2026, 3, 10), FechaVencimiento = new(2026, 6, 10), Monto = 95000000, Estado = "Pendiente" },
            new() { Id = 5, Factura = "F005-2026", Cliente = "EPM S.A.", RazonSocial = "EPM S.A.", Nit = "", FechaEmision = new(2026, 3, 15), FechaVencimiento = new(2026, 5, 30), Monto = 150000000, Estado = "Pagada" },
            new() { Id = 6, Factura = "F006-2026", Cliente = "Bancolombia S.A.", RazonSocial = "Bancolombia S.A.", Nit = "", FechaEmision = new(2026, 4, 1), FechaVencimiento = new(2026, 6, 15), Monto = 220000000, Estado = "Pendiente" },
            new() { Id = 7, Factura = "F007-2026", Cliente = "Softtek Colombia S.A.S.", RazonSocial = "Softtek Colombia S.A.S.", Nit = "", FechaEmision = new(2026, 4, 5), FechaVencimiento = new(2026, 7, 5), Monto = 75000000, Estado = "Pendiente" },
            new() { Id = 8, Factura = "F008-2026", Cliente = "Davivienda S.A.", RazonSocial = "Davivienda S.A.", Nit = "", FechaEmision = new(2026, 5, 1), FechaVencimiento = new(2026, 7, 20), Monto = 310000000, Estado = "Vencida" },
            new() { Id = 9, Factura = "F009-2026", Cliente = "Grupo Éxito S.A.S.", RazonSocial = "Grupo Éxito S.A.S.", Nit = "", FechaEmision = new(2026, 5, 10), FechaVencimiento = new(2026, 8, 10), Monto = 140000000, Estado = "Pendiente" },
            new() { Id = 10, Factura = "F010-2026", Cliente = "Claro Colombia S.A.", RazonSocial = "Claro Colombia S.A.", Nit = "", FechaEmision = new(2026, 6, 1), FechaVencimiento = new(2026, 8, 15), Monto = 180000000, Estado = "Pendiente" },
        ];
    }

    private static List<ProgramacionPagoDto> MockProgramacionPagos()
    {
        return
        [
            new() { Id = 1, Factura = "F001-2026", Cliente = "Bancolombia S.A.", Monto = 120000000, FechaVencimiento = new(2026, 1, 15), FechaCompromiso = "2026-04-15", Dias = 90, Categoria = "61-90 dias" },
            new() { Id = 2, Factura = "F002-2026", Cliente = "Grupo Éxito S.A.S.", Monto = 85000000, FechaVencimiento = new(2026, 2, 1), FechaCompromiso = "2026-04-01", Dias = 59, Categoria = "31-60 dias" },
            new() { Id = 3, Factura = "F003-2026", Cliente = "Davivienda S.A.", Monto = 200000000, FechaVencimiento = new(2026, 2, 20), FechaCompromiso = "2026-03-30", Dias = 38, Categoria = "31-60 dias" },
            new() { Id = 4, Factura = "F004-2026", Cliente = "Claro Colombia S.A.", Monto = 95000000, FechaVencimiento = new(2026, 4, 15), FechaCompromiso = "2026-04-10", Dias = -5, Categoria = "En Tiempo" },
            new() { Id = 5, Factura = "F005-2026", Cliente = "EPM S.A.", Monto = 150000000, FechaVencimiento = new(2026, 4, 30), FechaCompromiso = "2026-05-05", Dias = 5, Categoria = "0-15 dias" },
            new() { Id = 6, Factura = "F006-2026", Cliente = "Softtek Colombia S.A.S.", Monto = 75000000, FechaVencimiento = new(2026, 1, 5), FechaCompromiso = "2026-03-15", Dias = 69, Categoria = "61-90 dias" },
            new() { Id = 7, Factura = "F007-2026", Cliente = "Davivienda S.A.", Monto = 310000000, FechaVencimiento = new(2026, 5, 20), FechaCompromiso = "2026-05-25", Dias = 5, Categoria = "0-15 dias" },
            new() { Id = 8, Factura = "F008-2026", Cliente = "Bancolombia S.A.", Monto = 220000000, FechaVencimiento = new(2026, 5, 15), FechaCompromiso = "2026-06-01", Dias = 17, Categoria = "16-30 dias" },
            new() { Id = 9, Factura = "F009-2026", Cliente = "Grupo Éxito S.A.S.", Monto = 140000000, FechaVencimiento = new(2026, 3, 1), FechaCompromiso = "2026-04-20", Dias = 50, Categoria = "31-60 dias" },
            new() { Id = 10, Factura = "F010-2026", Cliente = "Claro Colombia S.A.", Monto = 180000000, FechaVencimiento = new(2026, 6, 1), FechaCompromiso = "2026-06-15", Dias = 14, Categoria = "0-15 dias" },
            new() { Id = 11, Factura = "F011-2026", Cliente = "EPM S.A.", Monto = 98000000, FechaVencimiento = new(2026, 6, 10), FechaCompromiso = "2026-08-20", Dias = 71, Categoria = "61-90 dias" },
            new() { Id = 12, Factura = "F012-2026", Cliente = "Bancolombia S.A.", Monto = 450000000, FechaVencimiento = new(2026, 7, 1), FechaCompromiso = "2026-10-15", Dias = 106, Categoria = "91-120 dias" },
        ];
    }

    private static List<CarteraClienteDto> MockClientes()
    {
        var nombres = new[] { "Cliente A S.A.S.", "Cliente B S.A.", "Cliente C Ltda.", "Cliente D S.A.S.", "Cliente E S.A." };
        var nits = new[] { "900.123.456-7", "800.234.567-8", "700.345.678-9", "600.456.789-0", "500.567.890-1" };
        return nombres.Select((n, i) => new CarteraClienteDto
        {
            Cliente = n,
            Nit = nits[i],
            MontoTotal = Math.Round((decimal)(_rng.NextDouble() * 200000000 + 5000000), 2),
            FacturasPendientes = _rng.Next(1, 15),
        }).ToList();
    }

    private static List<ClienteDetalleDto> MockClientesDetalle()
    {
        var clientes = new[]
        {
            new ClienteDetalleDto
            {
                Id = 1, Nombre = "Cliente A S.A.S.", Nit = "900.123.456-7",
                Grupo = "Grupo A", Direccion = "Calle 123 #45-67", Ciudad = "Bogotá",
                Region = "Cundinamarca", Pais = "Colombia", CodigoPostal = "110111",
                Telefono = "+57 1 2345678", EmailContabilidad = "contabilidad@clientea.com",
                CondicionesPago = "30 días",
                ContactoContabilidad = "Carlos Pérez", ContactoTesoreria = "María López",
                ContactoFinanzas = "Juan Gómez", ContactoOperacion = "Ana Martínez",
                ContactoComercial = "Pedro Sánchez", ContactoCompras = "Luisa Fernández",
                Notas = [new NotaClienteDto { Id = 1, Autor = "Sistema", Fecha = DateTime.Now.AddDays(-5), Texto = "Cliente con buen historial de pago." }],
                Contactos = [new ContactoClienteDto { Id = 1, Nombre = "Carlos Pérez", Cargo = "Contador", Departamento = "Contabilidad", Email = "carlos@clientea.com", Telefono = "+57 300 1234567" }],
            },
            new ClienteDetalleDto
            {
                Id = 2, Nombre = "Cliente B S.A.", Nit = "800.234.567-8",
                Grupo = "Grupo B", Direccion = "Av. Siempre Viva 742", Ciudad = "Medellín",
                Region = "Antioquia", Pais = "Colombia", CodigoPostal = "050001",
                Telefono = "+57 4 9876543", EmailContabilidad = "contabilidad@clienteb.com",
                CondicionesPago = "45 días",
                ContactoContabilidad = "Andrés Ruiz", ContactoTesoreria = "Laura García",
                ContactoFinanzas = "Diego Ramírez", ContactoOperacion = "Sofía Torres",
                ContactoComercial = "Felipe Ortiz", ContactoCompras = "Valentina Castro",
                Notas = [],
                Contactos = [],
            },
            new ClienteDetalleDto
            {
                Id = 3, Nombre = "Cliente C Ltda.", Nit = "700.345.678-9",
                Grupo = "Grupo C", Direccion = "Cra 50 #20-30", Ciudad = "Cali",
                Region = "Valle del Cauca", Pais = "Colombia", CodigoPostal = "760001",
                Telefono = "+57 2 5554433", EmailContabilidad = "contabilidad@clientec.com",
                CondicionesPago = "60 días",
                ContactoContabilidad = "Roberto Medina", ContactoTesoreria = "Carmen Díaz",
                ContactoFinanzas = "Luis Herrera", ContactoOperacion = "Patricia Rojas",
                ContactoComercial = "Jorge Vargas", ContactoCompras = "Diana Silva",
                Notas = [],
                Contactos = [],
            },
            new ClienteDetalleDto
            {
                Id = 4, Nombre = "Cliente D S.A.S.", Nit = "600.456.789-0",
                Grupo = "Grupo A", Direccion = "Transversal 12 #8-90", Ciudad = "Barranquilla",
                Region = "Atlántico", Pais = "Colombia", CodigoPostal = "080001",
                Telefono = "+57 5 3332211", EmailContabilidad = "contabilidad@cliented.com",
                CondicionesPago = "30 días",
                ContactoContabilidad = "Oscar Navarro", ContactoTesoreria = "Mónica Guzmán",
                ContactoFinanzas = "Ricardo Peña", ContactoOperacion = "Adriana Ríos",
                ContactoComercial = "Sergio Mora", ContactoCompras = "Natalia Cruz",
                Notas = [],
                Contactos = [],
            },
            new ClienteDetalleDto
            {
                Id = 5, Nombre = "Cliente E S.A.", Nit = "500.567.890-1",
                Grupo = "Grupo B", Direccion = "Calle 80 #30-45", Ciudad = "Bogotá",
                Region = "Cundinamarca", Pais = "Colombia", CodigoPostal = "110221",
                Telefono = "+57 1 4445566", EmailContabilidad = "contabilidad@clientee.com",
                CondicionesPago = "15 días",
                ContactoContabilidad = "Javier Soto", ContactoTesoreria = "Daniela Pardo",
                ContactoFinanzas = "Andrés Mendoza", ContactoOperacion = "Camila Vega",
                ContactoComercial = "Federico Rincón", ContactoCompras = "Gabriela Suárez",
                Notas = [],
                Contactos = [],
            },
        };
        return [.. clientes];
    }

    private static string ObtenerCategoria(ReporteCarteraFactura r)
    {
        if (r.VencidoEnTiempo != 0m) return "En Tiempo";
        if (r.Vencido0_15 != 0m) return "0-15 dias";
        if (r.Vencido16_30 != 0m) return "16-30 dias";
        if (r.Vencido31_60 != 0m) return "31-60 dias";
        if (r.Vencido61_90 != 0m) return "61-90 dias";
        if (r.Vencido91_120 != 0m) return "91-120 dias";
        if (r.Vencido121_365 != 0m) return "121-365 dias";
        return string.Empty;
    }

    private static int CalcularDias(string fechaCompromiso, DateTime? fechaVencimiento)
    {
        var hoy = DateTime.UtcNow.Date;
        if (DateTime.TryParse(fechaCompromiso, out var compromiso))
            return (int)(hoy - compromiso.Date).TotalDays;
        if (fechaVencimiento.HasValue)
            return (int)(hoy - fechaVencimiento.Value.Date).TotalDays;
        return 0;
    }

    private static string FormatearSemana(string semana, DateTime? fechaRef)
    {
        if (string.IsNullOrWhiteSpace(semana)) return "";
        if (!int.TryParse(semana, out var numSemana) || !fechaRef.HasValue)
            return semana;

        var inicioMes = new DateTime(fechaRef.Value.Year, fechaRef.Value.Month, 1);
        var inicioSemana = inicioMes.AddDays((numSemana - 1) * 7);
        var finSemana = inicioSemana.AddDays(6);
        var nombreMes = inicioSemana.ToString("MMM", new System.Globalization.CultureInfo("es-CO")).TrimEnd('.').ToLower();

        return $"{inicioSemana.Day} al {finSemana.Day} de {nombreMes}";
    }

    // ════════════════════════════════════════════════════════════════════
    // Implementación de la interfaz
    // ════════════════════════════════════════════════════════════════════

    public async Task<ApiResponse<CarteraResumenDto>> GetResumenAsync(string? moneda = null, string? cliente = null)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            var mock = MockFacturas();
            if (!string.IsNullOrWhiteSpace(cliente))
                mock = mock.Where(f => f.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            return ApiResponse<CarteraResumenDto>.Ok(new CarteraResumenDto
            {
                FacturasPorCobrar = mock.Where(f => f.Estado is "Pendiente" or "Vencida" or "Confirmada").Sum(f => f.Monto),
                FacturasVencidas = mock.Where(f => f.Estado == "Vencida").Sum(f => f.Monto),
                FacturasConfirmadas = mock.Where(f => f.Estado == "Confirmada").Sum(f => f.Monto),
                FacturasConDiferencia = _rng.Next(0, 5),
                Moneda = moneda ?? "COP",
            });
        }

        var rows = await _db.ReporteCarteraFacturas.Where(r => r.CargaArchivoId == cargaId).ToListAsync();

        var filtrados = string.IsNullOrWhiteSpace(moneda)
            ? rows
            : rows.Where(r => r.MonedaDocumento.Equals(moneda, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(cliente))
            filtrados = filtrados.Where(r => r.Deudor.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

        return ApiResponse<CarteraResumenDto>.Ok(new CarteraResumenDto
        {
            FacturasPorCobrar = filtrados.Sum(r => r.ValorRecibir),
            FacturasVencidas = filtrados.Sum(r => r.Vencido0_15 + r.Vencido16_30 + r.Vencido31_60 + r.Vencido61_90 + r.Vencido91_120 + r.Vencido121_365),
            FacturasConfirmadas = 0,
            FacturasConDiferencia = filtrados.Count(r => r.ImporteMonedaLocal > 0 && Math.Abs(r.ImporteMonedaLocal - r.ValorRecibir) > 1),
            Moneda = moneda ?? "COP",
        });
    }

    public async Task<ApiResponse<List<CarteraNotificacionDto>>> GetNotificacionesAsync()
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            var mock = MockFacturas();
            var notis = mock.Where(f => f.Estado is "Vencida" or "Pendiente")
                .Take(8)
                .Select(f => new CarteraNotificacionDto
                {
                    Id = f.Id,
                    Tipo = f.Estado == "Vencida" ? "Vencida" : "PorVencer",
                    Cliente = f.Cliente,
                    Factura = f.Factura,
                    FechaVencimiento = f.FechaVencimiento,
                    DiasMora = f.DiasMora,
                    Monto = f.Monto,
                }).ToList();
            return ApiResponse<List<CarteraNotificacionDto>>.Ok(notis);
        }

        var rows = await _db.ReporteCarteraFacturas
            .Where(r => r.CargaArchivoId == cargaId && !string.IsNullOrWhiteSpace(r.Estado))
            .Take(20)
            .ToListAsync();

        var notificaciones = rows.Select((r, i) => new CarteraNotificacionDto
        {
            Id = i + 1,
            Tipo = r.Estado.Equals("Vencida", StringComparison.OrdinalIgnoreCase) ? "Vencida" : "PorVencer",
            Cliente = r.Deudor,
            Factura = r.Referencia,
            FechaVencimiento = r.FechaPago ?? default,
            DiasMora = r.DemoraDPP1,
            Monto = r.ValorRecibir,
        }).ToList();

        return ApiResponse<List<CarteraNotificacionDto>>.Ok(notificaciones);
    }

    public async Task<ApiResponse<List<CarteraClienteDto>>> GetCarteraPorClienteAsync(string? moneda = null, string? cliente = null)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
            return ApiResponse<List<CarteraClienteDto>>.Ok(MockClientes());

        var rows = await _db.ReporteCarteraFacturas.Where(r => r.CargaArchivoId == cargaId).ToListAsync();

        if (!string.IsNullOrWhiteSpace(cliente))
            rows = rows.Where(r => r.Deudor.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

        var agrupados = rows
            .GroupBy(r => r.Deudor)
            .Select(g => new CarteraClienteDto
            {
                Cliente = g.Key,
                Nit = g.First().Asignacion,
                MontoTotal = g.Sum(r => r.ValorRecibir),
                FacturasPendientes = g.Count(r => !r.Estado.Equals("Pagada", StringComparison.OrdinalIgnoreCase)),
            })
            .Where(c => !string.IsNullOrWhiteSpace(c.Cliente))
            .OrderByDescending(c => c.MontoTotal)
            .ToList();

        return ApiResponse<List<CarteraClienteDto>>.Ok(agrupados);
    }

    public async Task<ApiResponse<List<CarteraClienteDto>>> GetCarteraPorCategoriaAsync(string? moneda = null, string? cliente = null)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            var mock = MockClientes();
            if (!string.IsNullOrWhiteSpace(cliente))
                mock = mock.Where(c => c.Nit.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            return ApiResponse<List<CarteraClienteDto>>.Ok(mock);
        }

        var rows = await _db.ReporteCarteraFacturas.Where(r => r.CargaArchivoId == cargaId).ToListAsync();

        if (!string.IsNullOrWhiteSpace(moneda))
            rows = rows.Where(r => r.MonedaDocumento.Equals(moneda, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(cliente))
            rows = rows.Where(r => r.Deudor.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

        var agrupados = rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Asignacion) ? r.Deudor : r.Asignacion)
            .Select(g => new CarteraClienteDto
            {
                Cliente = !string.IsNullOrWhiteSpace(g.First().RazonSocial) ? g.First().RazonSocial : g.First().Deudor,
                Nit = g.Key,
                MontoTotal = g.Sum(r => r.ValorRecibir),
                FacturasPendientes = g.Count(),
            })
            .Where(c => c.MontoTotal > 0)
            .OrderByDescending(c => c.MontoTotal)
            .ToList();

        return ApiResponse<List<CarteraClienteDto>>.Ok(agrupados);
    }

    public async Task<ApiResponse<List<ProyeccionPagoDto>>> GetProyeccionPagosAsync(string? moneda = null, string? cliente = null)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            var proyecciones = new List<ProyeccionPagoDto>
            {
                new() { RazonSocial = "Bancolombia S.A.", Monto = 340000000, FacturasPendientes = 3 },
                new() { RazonSocial = "Davivienda S.A.", Monto = 510000000, FacturasPendientes = 2 },
                new() { RazonSocial = "Grupo Éxito S.A.S.", Monto = 85000000, FacturasPendientes = 1 },
                new() { RazonSocial = "Claro Colombia S.A.", Monto = 95000000, FacturasPendientes = 1 },
                new() { RazonSocial = "EPM S.A.", Monto = 150000000, FacturasPendientes = 1 },
                new() { RazonSocial = "Softtek Colombia S.A.S.", Monto = 75000000, FacturasPendientes = 1 },
            };
            return ApiResponse<List<ProyeccionPagoDto>>.Ok(proyecciones);
        }

        var rows = await _db.ReporteCarteraFacturas
            .Where(r => r.CargaArchivoId == cargaId)
            .ToListAsync();

        var proyeccionesDb = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.RazonSocial) && !string.IsNullOrWhiteSpace(r.Estado) && r.Estado.Trim().Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.RazonSocial)
            .Select(g => new ProyeccionPagoDto
            {
                RazonSocial = g.Key,
                Monto = g.Sum(r => r.ValorRecibir),
                FacturasPendientes = g.Count(),
            })
            .Where(p => p.Monto > 0)
            .OrderByDescending(p => p.Monto)
            .ToList();

        return ApiResponse<List<ProyeccionPagoDto>>.Ok(proyeccionesDb);
    }

    public async Task<ApiResponse<List<SeguimientoUrgenteDto>>> GetSeguimientoUrgenteAsync(string? cliente = null)
    {
        var facturas = await GetFacturasAsync(cliente: cliente);
        if (!facturas.Success || facturas.Data is null)
            return ApiResponse<List<SeguimientoUrgenteDto>>.Ok([]);

        var urgentes = facturas.Data
            .Where(f => f.DiasMora > 60 || f.Estado == "Vencida")
            .GroupBy(f => f.Cliente)
            .Select(g => new SeguimientoUrgenteDto
            {
                Cliente = g.Key,
                RazonSocial = g.First().RazonSocial,
                Facturas = g.OrderByDescending(f => f.DiasMora).ToList(),
            })
            .Where(s => s.Facturas.Count > 0)
            .OrderByDescending(s => s.Facturas.Count)
            .ToList();

        return ApiResponse<List<SeguimientoUrgenteDto>>.Ok(urgentes);
    }

    public async Task<ApiResponse<List<CarteraFacturaDto>>> GetHistoricoFacturasAsync(string? cliente = null)
    {
        var cargaId = await UltimaCargaIdAsync("control-facturas");
        if (cargaId is null)
        {
            var mock = MockHistoricoFacturas();
            if (!string.IsNullOrWhiteSpace(cliente))
                mock = mock.Where(f => f.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            return ApiResponse<List<CarteraFacturaDto>>.Ok(mock);
        }

        var rows = await _db.ControlFacturas.Where(r => r.CargaArchivoId == cargaId).ToListAsync();

        var baseCargaId = await UltimaCargaIdAsync("base-clientes");
        Dictionary<string, string> clienteLookup = new(StringComparer.OrdinalIgnoreCase);
        if (baseCargaId is not null)
        {
            var bases = await _db.BaseClientes.Where(b => b.CargaArchivoId == baseCargaId).ToListAsync();
            foreach (var b in bases)
            {
                if (!string.IsNullOrWhiteSpace(b.NumeroCuenta) && !clienteLookup.ContainsKey(b.NumeroCuenta))
                    clienteLookup[b.NumeroCuenta] = b.NombreCliente;
            }
        }

        var facturas = rows
            .Select((r, i) => new CarteraFacturaDto
            {
                Id = i + 1,
                Factura = r.NumeroDocumento,
                Consecutivo = r.OrdenConsecutivo,
                Cliente = clienteLookup.TryGetValue(r.NumeroCliente, out var nombre) ? nombre : r.Cliente,
                RazonSocial = clienteLookup.TryGetValue(r.NumeroCliente, out var nombre2) ? nombre2 : r.Cliente,
                Nit = r.NumeroCliente,
                FechaEmision = r.FechaEmision ?? default,
                FechaVencimiento = r.FechaPago ?? default,
                Monto = r.Total,
                Retencion = r.Retencion,
                Estado = r.Estado,
            })
            .Where(f => !string.IsNullOrWhiteSpace(f.Factura))
            .ToList();

        if (!string.IsNullOrWhiteSpace(cliente))
            facturas = facturas.Where(f => f.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

        return ApiResponse<List<CarteraFacturaDto>>.Ok(facturas);
    }

    public async Task<ApiResponse<List<ProgramacionPagoDto>>> GetProgramacionPagosAsync(string? cliente = null)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            var mock = MockProgramacionPagos();
            if (!string.IsNullOrWhiteSpace(cliente))
                mock = mock.Where(p => p.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            return ApiResponse<List<ProgramacionPagoDto>>.Ok(mock);
        }

        var rows = await _db.ReporteCarteraFacturas.Where(r => r.CargaArchivoId == cargaId).ToListAsync();

        var pagos = rows
            .Select((r, i) =>
            {
                var categoria = ObtenerCategoria(r);
                return new { Row = r, Idx = i, Categoria = categoria };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Categoria))
            .Select(x => new ProgramacionPagoDto
            {
                Id = x.Idx + 1,
                Factura = x.Row.Referencia,
                Cliente = x.Row.RazonSocial,
                Monto = x.Row.ValorRecibir,
                FechaVencimiento = x.Row.FechaPago ?? default,
                FechaCompromiso = x.Row.FechaCompromiso,
                Dias = CalcularDias(x.Row.FechaCompromiso, x.Row.FechaPago),
                Categoria = x.Categoria,
                SemanaFormateada = FormatearSemana(x.Row.Semana, x.Row.FechaPago),
                ImporteMonedaLocal = x.Row.ImporteMonedaLocal,
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.Factura) && !string.IsNullOrWhiteSpace(p.Cliente))
            .ToList();

        if (!string.IsNullOrWhiteSpace(cliente))
            pagos = pagos.Where(p => p.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

        return ApiResponse<List<ProgramacionPagoDto>>.Ok(pagos);
    }

    public async Task<ApiResponse<byte[]>> ExportarProgramacionPagosAsync(string? cliente = null)
    {
        var resultado = await GetProgramacionPagosAsync(cliente);
        if (!resultado.Success || resultado.Data is null || resultado.Data.Count == 0)
            return ApiResponse<byte[]>.Fail("No hay datos para exportar.");

        var exportData = resultado.Data.Select(p => new
        {
            Factura = p.Factura,
            Cliente = p.Cliente,
            Monto = p.Monto,
            FechaVencimiento = p.FechaVencimiento,
            FechaCompromiso = p.FechaCompromiso,
            Dias = p.Dias,
            Categoria = p.Categoria
        }).ToList();

        var stream = new MemoryStream();
        await stream.SaveAsAsync(exportData);
        return ApiResponse<byte[]>.Ok(stream.ToArray());
    }

    public Task<ApiResponse<string>> EnviarAlertaAsync(string tipo, string cliente)
    {
        _logger.LogInformation("Alerta enviada: tipo={Tipo}, cliente={Cliente}", tipo, cliente);
        return Task.FromResult(ApiResponse<string>.Ok($"Alerta {tipo} enviada a {cliente}."));
    }

    public async Task<ApiResponse<List<CarteraFacturaDto>>> GetFacturasAsync(string? cliente = null, string? nit = null, string? estado = null)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            var mock = MockFacturas();
            if (!string.IsNullOrWhiteSpace(cliente))
                mock = mock.Where(f => f.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase) || f.Nit.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(estado))
                mock = mock.Where(f => f.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
            return ApiResponse<List<CarteraFacturaDto>>.Ok(mock);
        }

        var rows = await _db.ReporteCarteraFacturas.Where(r => r.CargaArchivoId == cargaId).ToListAsync();

        var facturas = rows
            .Select((r, i) => MapToFacturaDto(r, i + 1))
            .Where(f => !string.IsNullOrWhiteSpace(f.Factura))
            .ToList();

        if (!string.IsNullOrWhiteSpace(cliente) && !string.IsNullOrWhiteSpace(nit))
        {
            var busqueda = cliente;
            facturas = facturas.Where(f =>
                f.Cliente.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                f.Nit.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(cliente))
                facturas = facturas.Where(f => f.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(nit))
                facturas = facturas.Where(f => f.Nit.Contains(nit, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(estado))
            facturas = facturas.Where(f => f.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();

        return ApiResponse<List<CarteraFacturaDto>>.Ok(facturas);
    }

    private static CarteraFacturaDto MapToFacturaDto(ReporteCarteraFactura r, int id)
    {
        var cliente = string.IsNullOrWhiteSpace(r.Deudor) ? r.RazonSocial : r.Deudor;
        return new CarteraFacturaDto
        {
            Id = id,
            Factura = r.Referencia,
            Cliente = cliente,
            RazonSocial = r.RazonSocial,
            Nit = r.Asignacion,
            FechaEmision = r.FechaDocumento ?? default,
            FechaVencimiento = r.FechaPago ?? default,
            Monto = r.ValorRecibir != 0m ? r.ValorRecibir : r.ImporteMonedaLocal,
            Estado = r.Estado,
            DiasMora = r.DemoraDPP1,
        };
    }

    public async Task<ApiResponse<byte[]>> DescargarReporteFacturasAsync(string? cliente = null, string? nit = null, string? estado = null)
    {
        var facturas = await GetFacturasAsync(cliente, nit, estado);
        if (!facturas.Success || facturas.Data is null)
            return ApiResponse<byte[]>.Fail("No hay datos para generar el reporte.");

        var stream = new MemoryStream();
        await stream.SaveAsAsync(facturas.Data);
        return ApiResponse<byte[]>.Ok(stream.ToArray());
    }

    public async Task<ApiResponse<ComentarioDto>> AgregarComentarioAsync(int facturaId, string texto, DateTime? nuevaFechaCompromiso = null)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            _logger.LogWarning("No hay carga activa de tipo reporte-cartera, se retorna mock");
            return ApiResponse<ComentarioDto>.Ok(new ComentarioDto
            {
                Id = _rng.Next(100, 9999),
                Autor = "Usuario",
                Fecha = DateTime.Now,
                Texto = texto,
                NuevaFechaCompromiso = nuevaFechaCompromiso,
            });
        }

        var entity = new ComentarioFactura
        {
            CargaArchivoId = cargaId.Value,
            FacturaId = facturaId,
            Autor = "Usuario",
            Fecha = DateTime.UtcNow,
            Texto = texto,
            NuevaFechaCompromiso = nuevaFechaCompromiso,
        };

        _db.ComentariosFacturas.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al guardar comentario en BD: {Message} | Inner: {Inner}",
                ex.Message, ex.InnerException?.Message);
            return ApiResponse<ComentarioDto>.Fail(
                $"Error al guardar en base de datos: {ex.InnerException?.Message ?? ex.Message}");
        }

        _logger.LogInformation("Comentario {ComentarioId} agregado a factura {FacturaId}", entity.Id, facturaId);

        return ApiResponse<ComentarioDto>.Ok(new ComentarioDto
        {
            Id = entity.Id,
            Autor = entity.Autor,
            Fecha = entity.Fecha,
            Texto = entity.Texto,
            NuevaFechaCompromiso = entity.NuevaFechaCompromiso,
        });
    }

    public async Task<ApiResponse<List<ComentarioDto>>> GetComentariosAsync(int facturaId)
    {
        var cargaId = await UltimaCargaIdAsync("reporte-cartera");
        if (cargaId is null)
        {
            var comentarios = new List<ComentarioDto>
            {
                new() { Id = 1, Autor = "Sistema", Fecha = DateTime.Now.AddDays(-10), Texto = "Factura creada." },
                new() { Id = 2, Autor = "Usuario", Fecha = DateTime.Now.AddDays(-5), Texto = "Se solicitó confirmación de pago al cliente." },
                new() { Id = 3, Autor = "Usuario", Fecha = DateTime.Now.AddDays(-1), Texto = "Cliente confirmó pago para la próxima semana.", NuevaFechaCompromiso = DateTime.Now.AddDays(7) },
            };
            return ApiResponse<List<ComentarioDto>>.Ok(comentarios);
        }

        var entities = await _db.ComentariosFacturas
            .Where(c => c.CargaArchivoId == cargaId && c.FacturaId == facturaId)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();

        var result = entities.Select(e => new ComentarioDto
        {
            Id = e.Id,
            Autor = e.Autor,
            Fecha = e.Fecha,
            Texto = e.Texto,
            NuevaFechaCompromiso = e.NuevaFechaCompromiso,
        }).ToList();

        return ApiResponse<List<ComentarioDto>>.Ok(result);
    }

    public async Task<ApiResponse<TasaCambioDto>> GetTasaCambioAsync(string moneda = "USD")
    {
        var mon = moneda.ToUpperInvariant();
        var ultimo = await _db.TiposCambio
            .Where(t => t.Moneda == mon)
            .OrderByDescending(t => t.Año)
            .ThenByDescending(t => t.Mes)
            .FirstOrDefaultAsync();

        if (ultimo is null)
        {
            var tasaDefault = mon switch
            {
                "USD" => 4200m,
                "EUR" => 4600m,
                _ => 1m,
            };
            return ApiResponse<TasaCambioDto>.Ok(new TasaCambioDto { Moneda = mon, Tasa = tasaDefault });
        }

        return ApiResponse<TasaCambioDto>.Ok(new TasaCambioDto { Moneda = ultimo.Moneda, Tasa = ultimo.Tasa });
    }

    public Task<ApiResponse<List<NotificacionEnviadaDto>>> GetNotificacionesEnviadasAsync(string? estado = null, string? cliente = null)
    {
        var notis = new List<NotificacionEnviadaDto>();

        if (!string.IsNullOrWhiteSpace(estado))
            notis = notis.Where(n => n.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(cliente))
            notis = notis.Where(n => n.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult(ApiResponse<List<NotificacionEnviadaDto>>.Ok(notis));
    }

    public Task<ApiResponse<string>> EnviarRecordatorioAsync(List<int> facturasIds)
    {
        _logger.LogInformation("Recordatorio enviado para facturas: {Ids}", string.Join(", ", facturasIds));
        return Task.FromResult(ApiResponse<string>.Ok($"Recordatorio enviado para {facturasIds.Count} factura(s)."));
    }

    public async Task<ApiResponse<List<ClienteDetalleDto>>> GetClientesAsync(string? busqueda = null)
    {
        var cargaId = await UltimaCargaIdAsync("base-clientes");
        List<ClienteDetalleDto> clientes;

        if (cargaId is null)
        {
            clientes = MockClientesDetalle();
        }
        else
        {
            var rows = await _db.BaseClientes.Include(r => r.Contactos).Where(r => r.CargaArchivoId == cargaId).ToListAsync();
            clientes = rows.Select((r, i) => MapEntityToClienteDetalle(r, i + 1))
                .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
            clientes = clientes.Where(c =>
                c.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Nit.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();
        return ApiResponse<List<ClienteDetalleDto>>.Ok(clientes);
    }

    private ClienteDetalleDto MapEntityToClienteDetalle(BaseCliente e, int id)
    {
        return new ClienteDetalleDto
        {
            Id = id,
            Nombre = e.NombreCliente,
            Nit = e.NumeroCuenta,
            Grupo = e.GrupoCuenta,
            Direccion = e.Calle,
            Ciudad = "",
            Region = "",
            Pais = e.Pais,
            CodigoPostal = "",
            Telefono = e.Telefono,
            EmailContabilidad = e.CorreoContabilidad,
            CondicionesPago = "",
            ContactoContabilidad = e.ContactoContabilidad,
            ContactoTesoreria = e.ContactoTesoreria,
            ContactoFinanzas = e.ContactoFinanzas,
            ContactoOperacion = e.ContactoOperacion,
            ContactoComercial = e.ContactoComercial,
            ContactoCompras = e.ContactoCompras,
            Contactos = e.Contactos.Select(c => new ContactoClienteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Cargo = c.Cargo,
                Departamento = c.Departamento,
                Email = c.Email,
                Telefono = c.Telefono,
            }).ToList(),
        };
    }

    public async Task<ApiResponse<ClienteDetalleDto>> GetClienteByIdAsync(int id)
    {
        var cargaId = await UltimaCargaIdAsync("base-clientes");
        if (cargaId is null)
        {
            var mock = MockClientesDetalle();
            var mockCliente = mock.FirstOrDefault(c => c.Id == id);
            if (mockCliente is null)
                return ApiResponse<ClienteDetalleDto>.Fail($"Cliente con ID {id} no encontrado.");
            return ApiResponse<ClienteDetalleDto>.Ok(mockCliente);
        }

        var rows = await _db.BaseClientes.Include(r => r.Contactos).Where(r => r.CargaArchivoId == cargaId).ToListAsync();
        var clientes = rows.Select((r, i) => MapEntityToClienteDetalle(r, i + 1))
            .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
            .ToList();

        var cliente = clientes.FirstOrDefault(c => c.Id == id);
        if (cliente is null)
            return ApiResponse<ClienteDetalleDto>.Fail($"Cliente con ID {id} no encontrado.");

        return ApiResponse<ClienteDetalleDto>.Ok(cliente);
    }

    public Task<ApiResponse<NotaClienteDto>> AgregarNotaClienteAsync(int clienteId, string texto)
    {
        _logger.LogInformation("Nota agregada al cliente {ClienteId}: {Texto}", clienteId, texto);
        return Task.FromResult(ApiResponse<NotaClienteDto>.Ok(new NotaClienteDto
        {
            Id = _rng.Next(100, 9999),
            Autor = "Usuario",
            Fecha = DateTime.Now,
            Texto = texto,
        }));
    }

    public async Task<ApiResponse<ContactoClienteDto>> AgregarContactoAsync(int clienteId, ContactoClienteDto contacto)
    {
        var cargaId = await UltimaCargaIdAsync("base-clientes");
        if (cargaId is null)
            return ApiResponse<ContactoClienteDto>.Fail("No hay carga de base de clientes activa.");

        var rows = await _db.BaseClientes.Where(r => r.CargaArchivoId == cargaId).ToListAsync();
        if (clienteId < 1 || clienteId > rows.Count)
            return ApiResponse<ContactoClienteDto>.Fail("Cliente no encontrado.");

        var baseCliente = rows[clienteId - 1];
        var entity = new ContactoCliente
        {
            BaseClienteId = baseCliente.Id,
            Nombre = contacto.Nombre,
            Cargo = contacto.Cargo ?? string.Empty,
            Departamento = contacto.Departamento ?? string.Empty,
            Email = contacto.Email,
            Telefono = contacto.Telefono ?? string.Empty,
        };

        _db.ContactosClientes.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Contacto {ContactoId} agregado al cliente {ClienteId}", entity.Id, clienteId);

        return ApiResponse<ContactoClienteDto>.Ok(new ContactoClienteDto
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Cargo = entity.Cargo,
            Departamento = entity.Departamento,
            Email = entity.Email,
            Telefono = entity.Telefono,
        });
    }

    public async Task<ApiResponse<ContactoClienteDto>> ActualizarContactoAsync(int clienteId, int contactoId, ContactoClienteDto contacto)
    {
        var cargaId = await UltimaCargaIdAsync("base-clientes");
        if (cargaId is null)
            return ApiResponse<ContactoClienteDto>.Fail("No hay carga de base de clientes activa.");

        var baseIds = await _db.BaseClientes
            .Where(r => r.CargaArchivoId == cargaId)
            .Select(r => r.Id)
            .ToListAsync();

        if (clienteId < 1 || clienteId > baseIds.Count)
            return ApiResponse<ContactoClienteDto>.Fail("Cliente no encontrado.");

        var entity = await _db.ContactosClientes
            .FirstOrDefaultAsync(c => c.Id == contactoId && c.BaseClienteId == baseIds[clienteId - 1]);

        if (entity is null)
            return ApiResponse<ContactoClienteDto>.Fail("Contacto no encontrado.");

        entity.Nombre = contacto.Nombre;
        entity.Cargo = contacto.Cargo ?? string.Empty;
        entity.Departamento = contacto.Departamento ?? string.Empty;
        entity.Email = contacto.Email;
        entity.Telefono = contacto.Telefono ?? string.Empty;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Contacto {ContactoId} actualizado", contactoId);

        return ApiResponse<ContactoClienteDto>.Ok(new ContactoClienteDto
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Cargo = entity.Cargo,
            Departamento = entity.Departamento,
            Email = entity.Email,
            Telefono = entity.Telefono,
        });
    }

    public async Task<ApiResponse<string>> EliminarContactoAsync(int clienteId, int contactoId)
    {
        var cargaId = await UltimaCargaIdAsync("base-clientes");
        if (cargaId is null)
            return ApiResponse<string>.Fail("No hay carga de base de clientes activa.");

        var baseIds = await _db.BaseClientes
            .Where(r => r.CargaArchivoId == cargaId)
            .Select(r => r.Id)
            .ToListAsync();

        if (clienteId < 1 || clienteId > baseIds.Count)
            return ApiResponse<string>.Fail("Cliente no encontrado.");

        var entity = await _db.ContactosClientes
            .FirstOrDefaultAsync(c => c.Id == contactoId && c.BaseClienteId == baseIds[clienteId - 1]);

        if (entity is null)
            return ApiResponse<string>.Fail("Contacto no encontrado.");

        _db.ContactosClientes.Remove(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Contacto {ContactoId} eliminado", contactoId);

        return ApiResponse<string>.Ok("Contacto eliminado correctamente.");
    }

    public Task<ApiResponse<SubProyectoResumenDto>> GetSubProyectosResumenAsync()
    {
        return Task.FromResult(ApiResponse<SubProyectoResumenDto>.Ok(new SubProyectoResumenDto
        {
            TotalProyectos = 25,
            ProyectosActivos = 18,
            PendientesDocumentacion = 4,
        }));
    }

    public Task<ApiResponse<List<SubProyectoDto>>> GetSubProyectosListaAsync()
    {
        var proyectos = new List<SubProyectoDto>
        {
            new() { Id = 1, Nombre = "Implementación SAP", Codigo = "PROY-001", Empresa = "Softtek", Cliente = "Cliente A S.A.S.", Estado = "Activo", Valor = 150000000 },
            new() { Id = 2, Nombre = "Migración Cloud", Codigo = "PROY-002", Empresa = "Softtek", Cliente = "Cliente B S.A.", Estado = "Activo", Valor = 85000000 },
            new() { Id = 3, Nombre = "Desarrollo App Móvil", Codigo = "PROY-003", Empresa = "Softtek", Cliente = "Cliente C Ltda.", Estado = "Pendiente Documentación", Valor = 120000000 },
            new() { Id = 4, Nombre = "Consultoría TI", Codigo = "PROY-004", Empresa = "Softtek", Cliente = "Cliente D S.A.S.", Estado = "Activo", Valor = 45000000 },
            new() { Id = 5, Nombre = "Soporte Técnico", Codigo = "PROY-005", Empresa = "Softtek", Cliente = "Cliente E S.A.", Estado = "Completado", Valor = 30000000 },
        };
        return Task.FromResult(ApiResponse<List<SubProyectoDto>>.Ok(proyectos));
    }

    public Task<ApiResponse<List<DirectorioEmpresaDto>>> GetDirectorioEmpresaAsync(string empresa)
    {
        var directorio = new List<DirectorioEmpresaDto>
        {
            new() { Contacto = "Juan Pérez", Cargo = "Gerente de Cuenta", Email = "jperez@softtek.com", Telefono = "+57 300 1112233" },
            new() { Contacto = "María Gómez", Cargo = "Coordinadora Financiera", Email = "mgomez@softtek.com", Telefono = "+57 300 4445566" },
            new() { Contacto = "Carlos López", Cargo = "Analista de Cartera", Email = "clopez@softtek.com", Telefono = "+57 300 7778899" },
        };
        return Task.FromResult(ApiResponse<List<DirectorioEmpresaDto>>.Ok(directorio));
    }

    public Task<ApiResponse<List<string>>> GetEmpresasAsync()
    {
        return Task.FromResult(ApiResponse<List<string>>.Ok(new List<string>
        {
            "Softtek Colombia S.A.S.",
            "Softtek México S.A. de C.V.",
            "Softtek Brasil Ltda.",
            "Softtek Argentina S.A.",
        }));
    }

    public Task<ApiResponse<string>> UploadDirectorioAsync(string empresa, Stream fileStream, string fileName)
    {
        _logger.LogInformation("Directorio subido para empresa {Empresa}: {FileName}", empresa, fileName);
        return Task.FromResult(ApiResponse<string>.Ok($"Directorio de {empresa} actualizado correctamente."));
    }

    public Task<ApiResponse<List<FechaReprogramadaDto>>> GetFechasReprogramadasAsync()
    {
        var reprogramadas = new List<FechaReprogramadaDto>
        {
            new() { Factura = "F-2024-001", Cliente = "Bancolombia", NuevaFechaCompromiso = new DateTime(2026, 4, 15), Texto = "Se acordó nuevo plazo hasta el 15 de abril.", Autor = "Carlos Méndez", Fecha = new DateTime(2026, 3, 1, 14, 0, 0) },
            new() { Factura = "F-2024-003", Cliente = "Davivienda", NuevaFechaCompromiso = new DateTime(2026, 3, 30), Texto = "Cliente solicitó extensión de plazo por problemas de flujo de caja.", Autor = "María Torres", Fecha = new DateTime(2026, 2, 28, 10, 30, 0) },
            new() { Factura = "F-2024-007", Cliente = "Grupo Éxito", NuevaFechaCompromiso = new DateTime(2026, 5, 10), Texto = "Se reprograma pago según nuevo acuerdo comercial.", Autor = "Andrés García", Fecha = new DateTime(2026, 3, 5, 16, 0, 0) },
        };
        return Task.FromResult(ApiResponse<List<FechaReprogramadaDto>>.Ok(reprogramadas));
    }

    public Task<ApiResponse<string>> EnviarNotificacionBRMAsync(NotificacionBRMDto notificacion)
    {
        _logger.LogInformation("Notificación BRM enviada a {CorreoCliente} (BRM: {CorreoBRM}): {Asunto}",
            notificacion.CorreoCliente, notificacion.CorreoBRM, notificacion.Asunto);
        return Task.FromResult(ApiResponse<string>.Ok("Notificación BRM enviada correctamente."));
    }
}
