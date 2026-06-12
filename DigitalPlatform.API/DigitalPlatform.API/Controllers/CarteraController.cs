using System.Text.Json;
using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Cartera;
using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarteraController : ControllerBase
{
    private readonly ICarteraService _carteraService;

    public CarteraController(ICarteraService carteraService)
    {
        _carteraService = carteraService;
    }

    // GET api/cartera/resumen?moneda=COP&cliente=
    [HttpGet("resumen")]
    public async Task<ActionResult<ApiResponse<CarteraResumenDto>>> GetResumen(
        [FromQuery] string? moneda, [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetResumenAsync(moneda, cliente);
        return Ok(resultado);
    }

    // GET api/cartera/notificaciones
    [HttpGet("notificaciones")]
    public async Task<ActionResult<ApiResponse<List<CarteraNotificacionDto>>>> GetNotificaciones()
    {
        var resultado = await _carteraService.GetNotificacionesAsync();
        return Ok(resultado);
    }

    // GET api/cartera/por-cliente?moneda=COP&cliente=
    [HttpGet("cartera-por-cliente")]
    public async Task<ActionResult<ApiResponse<List<CarteraClienteDto>>>> GetCarteraPorCliente(
        [FromQuery] string? moneda, [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetCarteraPorClienteAsync(moneda, cliente);
        return Ok(resultado);
    }

    // GET api/cartera/cartera-por-categoria?moneda=COP&cliente=
    [HttpGet("cartera-por-categoria")]
    public async Task<ActionResult<ApiResponse<List<CarteraClienteDto>>>> GetCarteraPorCategoria(
        [FromQuery] string? moneda, [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetCarteraPorCategoriaAsync(moneda, cliente);
        return Ok(resultado);
    }

    // GET api/cartera/proyeccion-pagos?moneda=COP&cliente=
    [HttpGet("proyeccion-pagos")]
    public async Task<ActionResult<ApiResponse<List<ProyeccionPagoDto>>>> GetProyeccionPagos(
        [FromQuery] string? moneda, [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetProyeccionPagosAsync(moneda, cliente);
        return Ok(resultado);
    }

    // GET api/cartera/seguimiento-urgente?cliente=
    [HttpGet("seguimiento-urgente")]
    public async Task<ActionResult<ApiResponse<List<SeguimientoUrgenteDto>>>> GetSeguimientoUrgente(
        [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetSeguimientoUrgenteAsync(cliente);
        return Ok(resultado);
    }

    // GET api/cartera/historico-facturas?cliente=
    [HttpGet("historico-facturas")]
    public async Task<ActionResult<ApiResponse<List<CarteraFacturaDto>>>> GetHistoricoFacturas(
        [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetHistoricoFacturasAsync(cliente);
        return Ok(resultado);
    }

    // GET api/cartera/programacion-pagos?cliente=
    [HttpGet("programacion-pagos")]
    public async Task<ActionResult<ApiResponse<List<ProgramacionPagoDto>>>> GetProgramacionPagos(
        [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetProgramacionPagosAsync(cliente);
        return Ok(resultado);
    }

    // GET api/cartera/exportar-programacion?cliente=
    [HttpGet("exportar-programacion")]
    public async Task<IActionResult> ExportarProgramacionPagos([FromQuery] string? cliente)
    {
        var resultado = await _carteraService.ExportarProgramacionPagosAsync(cliente);
        if (!resultado.Success || resultado.Data is null)
            return BadRequest(resultado);
        return File(resultado.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "programacion_pagos.xlsx");
    }

    // POST api/cartera/enviar-alerta
    [HttpPost("enviar-alerta")]
    public async Task<ActionResult<ApiResponse<string>>> EnviarAlerta([FromQuery] string tipo, [FromQuery] string cliente)
    {
        var resultado = await _carteraService.EnviarAlertaAsync(tipo, cliente);
        return Ok(resultado);
    }

    // GET api/cartera/facturas?cliente=&nit=&estado=
    [HttpGet("facturas")]
    public async Task<ActionResult<ApiResponse<List<CarteraFacturaDto>>>> GetFacturas(
        [FromQuery] string? cliente, [FromQuery] string? nit, [FromQuery] string? estado)
    {
        var resultado = await _carteraService.GetFacturasAsync(cliente, nit, estado);
        return Ok(resultado);
    }

    // GET api/cartera/descargar-reporte?cliente=&nit=&estado=
    [HttpGet("descargar-reporte")]
    public async Task<IActionResult> DescargarReporte(
        [FromQuery] string? cliente, [FromQuery] string? nit, [FromQuery] string? estado)
    {
        var resultado = await _carteraService.DescargarReporteFacturasAsync(cliente, nit, estado);
        if (!resultado.Success || resultado.Data is null)
            return BadRequest(resultado);
        return File(resultado.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reporte_cartera.xlsx");
    }

    // POST api/cartera/facturas/{facturaId}/comentarios
    [HttpPost("facturas/{facturaId}/comentarios")]
    public async Task<ActionResult<ApiResponse<ComentarioDto>>> AgregarComentario(
        int facturaId, [FromBody] JsonElement body)
    {
        var texto = body.GetProperty("texto").GetString() ?? string.Empty;
        DateTime? nuevaFecha = null;
        if (body.TryGetProperty("nuevaFechaCompromiso", out var fechaProp) && fechaProp.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(fechaProp.GetString(), out var dt))
                nuevaFecha = dt;
        }
        var resultado = await _carteraService.AgregarComentarioAsync(facturaId, texto, nuevaFecha);
        return Ok(resultado);
    }

    // GET api/cartera/facturas/{facturaId}/comentarios
    [HttpGet("facturas/{facturaId}/comentarios")]
    public async Task<ActionResult<ApiResponse<List<ComentarioDto>>>> GetComentarios(int facturaId)
    {
        var resultado = await _carteraService.GetComentariosAsync(facturaId);
        return Ok(resultado);
    }

    // GET api/cartera/notificaciones-enviadas?estado=&cliente=
    [HttpGet("notificaciones-enviadas")]
    public async Task<ActionResult<ApiResponse<List<NotificacionEnviadaDto>>>> GetNotificacionesEnviadas(
        [FromQuery] string? estado, [FromQuery] string? cliente)
    {
        var resultado = await _carteraService.GetNotificacionesEnviadasAsync(estado, cliente);
        return Ok(resultado);
    }

    // POST api/cartera/enviar-recordatorio
    [HttpPost("enviar-recordatorio")]
    public async Task<ActionResult<ApiResponse<string>>> EnviarRecordatorio([FromBody] List<int> facturasIds)
    {
        var resultado = await _carteraService.EnviarRecordatorioAsync(facturasIds);
        return Ok(resultado);
    }

    // GET api/cartera/clientes?busqueda=
    [HttpGet("clientes")]
    public async Task<ActionResult<ApiResponse<List<ClienteDetalleDto>>>> GetClientes([FromQuery] string? busqueda)
    {
        var resultado = await _carteraService.GetClientesAsync(busqueda);
        return Ok(resultado);
    }

    // GET api/cartera/clientes/{id}
    [HttpGet("clientes/{id}")]
    public async Task<ActionResult<ApiResponse<ClienteDetalleDto>>> GetClienteById(int id)
    {
        var resultado = await _carteraService.GetClienteByIdAsync(id);
        return Ok(resultado);
    }

    // POST api/cartera/clientes/{id}/notas
    [HttpPost("clientes/{id}/notas")]
    public async Task<ActionResult<ApiResponse<NotaClienteDto>>> AgregarNotaCliente(int id, [FromBody] NotaClienteDto nota)
    {
        var resultado = await _carteraService.AgregarNotaClienteAsync(id, nota.Texto);
        return Ok(resultado);
    }

    // POST api/cartera/clientes/{id}/contactos
    [HttpPost("clientes/{id}/contactos")]
    public async Task<ActionResult<ApiResponse<ContactoClienteDto>>> AgregarContacto(int id, [FromBody] ContactoClienteDto contacto)
    {
        var resultado = await _carteraService.AgregarContactoAsync(id, contacto);
        return Ok(resultado);
    }

    // GET api/cartera/subproyectos/resumen
    [HttpGet("subproyectos/resumen")]
    public async Task<ActionResult<ApiResponse<SubProyectoResumenDto>>> GetSubProyectosResumen()
    {
        var resultado = await _carteraService.GetSubProyectosResumenAsync();
        return Ok(resultado);
    }

    // GET api/cartera/subproyectos/lista
    [HttpGet("subproyectos")]
    public async Task<ActionResult<ApiResponse<List<SubProyectoDto>>>> GetSubProyectosLista()
    {
        var resultado = await _carteraService.GetSubProyectosListaAsync();
        return Ok(resultado);
    }

    // GET api/cartera/directorio?empresa=
    [HttpGet("directorio/{empresa}")]
    public async Task<ActionResult<ApiResponse<List<DirectorioEmpresaDto>>>> GetDirectorio(string empresa)
    {
        var resultado = await _carteraService.GetDirectorioEmpresaAsync(empresa);
        return Ok(resultado);
    }

    // GET api/cartera/empresas
    [HttpGet("empresas")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetEmpresas()
    {
        var resultado = await _carteraService.GetEmpresasAsync();
        return Ok(resultado);
    }

    // POST api/cartera/upload-directorio?empresa=
    [HttpPost("directorio/{empresa}/upload")]
    public async Task<ActionResult<ApiResponse<string>>> UploadDirectorio(
        string empresa, IFormFile file)
    {
        if (file is null)
            return BadRequest(ApiResponse<string>.Fail("El archivo es requerido."));
        await using var stream = file.OpenReadStream();
        var resultado = await _carteraService.UploadDirectorioAsync(empresa, stream, file.FileName);
        return Ok(resultado);
    }

    // GET api/cartera/fechas-reprogramadas
    [HttpGet("fechas-reprogramadas")]
    public async Task<ActionResult<ApiResponse<List<FechaReprogramadaDto>>>> GetFechasReprogramadas()
    {
        var resultado = await _carteraService.GetFechasReprogramadasAsync();
        return Ok(resultado);
    }

    // POST api/cartera/enviar-notificacion-brm
    [HttpPost("enviar-notificacion-brm")]
    public async Task<ActionResult<ApiResponse<string>>> EnviarNotificacionBRM([FromBody] NotificacionBRMDto notificacion)
    {
        var resultado = await _carteraService.EnviarNotificacionBRMAsync(notificacion);
        return Ok(resultado);
    }
}
