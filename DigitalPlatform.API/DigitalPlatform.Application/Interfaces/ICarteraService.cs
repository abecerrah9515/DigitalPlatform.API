using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Cartera;

namespace DigitalPlatform.Application.Interfaces;

public interface ICarteraService
{
    Task<ApiResponse<CarteraResumenDto>> GetResumenAsync(string? moneda = null, string? cliente = null);
    Task<ApiResponse<List<CarteraNotificacionDto>>> GetNotificacionesAsync();
    Task<ApiResponse<List<CarteraClienteDto>>> GetCarteraPorClienteAsync(string? moneda = null, string? cliente = null);
    Task<ApiResponse<List<CarteraClienteDto>>> GetCarteraPorCategoriaAsync(string? moneda = null, string? cliente = null);
    Task<ApiResponse<List<ProyeccionPagoDto>>> GetProyeccionPagosAsync(string? moneda = null, string? cliente = null);
    Task<ApiResponse<List<SeguimientoUrgenteDto>>> GetSeguimientoUrgenteAsync(string? cliente = null);
    Task<ApiResponse<List<CarteraFacturaDto>>> GetHistoricoFacturasAsync(string? cliente = null);
    Task<ApiResponse<List<ProgramacionPagoDto>>> GetProgramacionPagosAsync(string? cliente = null);
    Task<ApiResponse<byte[]>> ExportarProgramacionPagosAsync(string? cliente = null);
    Task<ApiResponse<string>> EnviarAlertaAsync(string tipo, string cliente);
    Task<ApiResponse<List<CarteraFacturaDto>>> GetFacturasAsync(string? cliente = null, string? nit = null, string? estado = null);
    Task<ApiResponse<byte[]>> DescargarReporteFacturasAsync(string? cliente = null, string? nit = null, string? estado = null);
    Task<ApiResponse<ComentarioDto>> AgregarComentarioAsync(int facturaId, string texto, DateTime? nuevaFechaCompromiso = null);
    Task<ApiResponse<List<ComentarioDto>>> GetComentariosAsync(int facturaId);
    Task<ApiResponse<List<NotificacionEnviadaDto>>> GetNotificacionesEnviadasAsync(string? estado = null, string? cliente = null);
    Task<ApiResponse<string>> EnviarRecordatorioAsync(List<int> facturasIds);
    Task<ApiResponse<List<ClienteDetalleDto>>> GetClientesAsync(string? busqueda = null);
    Task<ApiResponse<ClienteDetalleDto>> GetClienteByIdAsync(int id);
    Task<ApiResponse<NotaClienteDto>> AgregarNotaClienteAsync(int clienteId, string texto);
    Task<ApiResponse<ContactoClienteDto>> AgregarContactoAsync(int clienteId, ContactoClienteDto contacto);
    Task<ApiResponse<SubProyectoResumenDto>> GetSubProyectosResumenAsync();
    Task<ApiResponse<List<SubProyectoDto>>> GetSubProyectosListaAsync();
    Task<ApiResponse<List<DirectorioEmpresaDto>>> GetDirectorioEmpresaAsync(string empresa);
    Task<ApiResponse<List<string>>> GetEmpresasAsync();
    Task<ApiResponse<string>> UploadDirectorioAsync(string empresa, Stream fileStream, string fileName);
    Task<ApiResponse<string>> EnviarNotificacionBRMAsync(NotificacionBRMDto notificacion);
    Task<ApiResponse<List<FechaReprogramadaDto>>> GetFechasReprogramadasAsync();
}
