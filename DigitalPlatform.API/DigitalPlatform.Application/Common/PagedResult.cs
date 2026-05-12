namespace DigitalPlatform.Application.Common;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalRegistros { get; set; }
    public int Pagina { get; set; }
    public int TamañoPagina { get; set; }
    public int TotalPaginas => (int)Math.Ceiling(TotalRegistros / (double)TamañoPagina);
}
