using System.Globalization;
using System.Text.RegularExpressions;
using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Pyl;
using DigitalPlatform.Application.Interfaces;
using DigitalPlatform.Domain.Enums;
using DigitalPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalPlatform.Infrastructure.Services;

/// <summary>
/// Arma la tabla P&L jerárquica (HUE-08/09): árbol de Accounts_Group con valores
/// mensuales (ENE–DIC) + ACUM. Hojas numéricas → GR55; agrupadores → suma de hijos;
/// nodos con fórmula → evaluar la columna Referencia.
/// </summary>
public class PnlService : IPnlService
{
    private readonly ApplicationDbContext _db;
    public PnlService(ApplicationDbContext db) => _db = db;

    private static readonly EstadoConsolidacion[] _estadosValidos =
        [EstadoConsolidacion.Exitoso, EstadoConsolidacion.ParcialmenteExitoso];

    private async Task<int?> ResolverConsolidacionAsync(int? consolidacionId) =>
        consolidacionId ?? await _db.ConsolidacionLogs
            .Where(l => _estadosValidos.Contains(l.Estado))
            .OrderByDescending(l => l.FechaInicio)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

    private static bool EsNumerico(string s) => s.Length > 0 && s.All(char.IsDigit);

    // ════════════════════════════════════════════════════════════════════════
    // GET /api/pyl
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<PnlResponseDto>> ObtenerPnlAsync(PnlFiltros f)
    {
        var moneda = (f.Moneda ?? "COP").ToUpperInvariant();

        // Sin año seleccionado → mensaje "Seleccione un año…" (HUE-07)
        if (f.Año is null or 0)
            return ApiResponse<PnlResponseDto>.Ok(new PnlResponseDto { RequiereAño = true, Moneda = moneda },
                "Seleccione un año para visualizar el estado de resultados.");

        var ultimoId = await ResolverConsolidacionAsync(f.ConsolidacionId);
        if (ultimoId is null)
            return ApiResponse<PnlResponseDto>.Ok(new PnlResponseDto { Año = f.Año, Moneda = moneda },
                "Sin consolidación disponible.");

        var año = f.Año.Value;

        // ── Árbol de cuentas ─────────────────────────────────────────────────
        var cuentas = await _db.CuentasPnl.AsNoTracking()
            .Where(c => c.ConsolidacionId == ultimoId)
            .OrderBy(c => c.Orden)
            .ToListAsync();
        if (cuentas.Count == 0)
            return ApiResponse<PnlResponseDto>.Ok(new PnlResponseDto { Año = año, Moneda = moneda },
                "La consolidación no tiene cuentas P&L.");

        var porId    = cuentas.ToDictionary(c => c.LineItemId, StringComparer.OrdinalIgnoreCase);
        var hijosDe  = cuentas.Where(c => !string.IsNullOrEmpty(c.ParentId))
                              .GroupBy(c => c.ParentId, StringComparer.OrdinalIgnoreCase)
                              .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Orden).ToList(), StringComparer.OrdinalIgnoreCase);
        var todosIds = cuentas.Select(c => c.LineItemId).ToList();

        // ── Movimientos GR55 filtrados → dict[cuenta] = valor[12] (× Factor) ──
        var movQuery = _db.MovimientosGR55.AsNoTracking()
            .Where(m => m.ConsolidacionId == ultimoId && m.Año == año);
        if (f.Cliente?.Length  > 0) movQuery = movQuery.Where(m => f.Cliente.Contains(m.Cliente));
        if (f.Proyecto?.Length > 0) movQuery = movQuery.Where(m => f.Proyecto.Contains(m.CodProyecto));
        if (f.Vertical?.Length > 0) movQuery = movQuery.Where(m => f.Vertical.Contains(m.Vertical));

        var movs = await movQuery
            .GroupBy(m => new { m.NumeroCuenta, m.Mes })
            .Select(g => new { g.Key.NumeroCuenta, g.Key.Mes, Valor = g.Sum(x => x.Valor) })
            .ToListAsync();

        // Factor por mes (COP → tasaCop, USD → 1)
        var factor = new decimal[13]; // 1..12
        for (int m = 1; m <= 12; m++) factor[m] = 1m;
        if (moneda == "COP")
        {
            var tasas = await _db.TiposCambio.AsNoTracking()
                .Where(t => t.Moneda == "COP" && t.Año == año)
                .ToDictionaryAsync(t => t.Mes, t => t.Tasa);
            var ultima = await _db.TiposCambio.AsNoTracking()
                .Where(t => t.Moneda == "COP")
                .OrderByDescending(t => t.Año).ThenByDescending(t => t.Mes)
                .Select(t => t.Tasa).FirstOrDefaultAsync();
            if (ultima == 0) ultima = 1m;
            for (int m = 1; m <= 12; m++)
                factor[m] = tasas.TryGetValue(m, out var t) && t > 0 ? t : ultima;
        }

        var hojaValores = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var mv in movs)
        {
            if (!hojaValores.TryGetValue(mv.NumeroCuenta, out var arr))
                hojaValores[mv.NumeroCuenta] = arr = new decimal[12];
            if (mv.Mes is >= 1 and <= 12)
                arr[mv.Mes - 1] += mv.Valor * factor[mv.Mes];
        }
        var cuentasConMov = new HashSet<string>(movs.Select(m => m.NumeroCuenta), StringComparer.OrdinalIgnoreCase);

        // ── Cálculo memoizado de valores mensuales por nodo ──────────────────
        var memoVal = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
        var enProceso = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        decimal[] ValoresDe(string id)
        {
            if (memoVal.TryGetValue(id, out var cached)) return cached;
            if (!porId.TryGetValue(id, out var nodo) || !enProceso.Add(id))
                return new decimal[12]; // desconocido o ciclo → 0

            var vals = new decimal[12];
            if (EsNumerico(nodo.LineItemId))
            {
                if (hojaValores.TryGetValue(nodo.LineItemId, out var hv)) Array.Copy(hv, vals, 12);
            }
            else if (!string.IsNullOrWhiteSpace(nodo.Referencia))
            {
                for (int m = 0; m < 12; m++)
                    vals[m] = EvaluarFormula(nodo.Referencia, refId => ValoresDe(refId)[m], todosIds);
            }
            else if (hijosDe.TryGetValue(id, out var hijos))
            {
                foreach (var h in hijos)
                {
                    var hv = ValoresDe(h.LineItemId);
                    for (int m = 0; m < 12; m++) vals[m] += hv[m];
                }
            }
            enProceso.Remove(id);
            memoVal[id] = vals;
            return vals;
        }

        // ACUM: fórmula → evaluar sobre acums; resto → suma de los 12 meses.
        var memoAcum = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var enProcAcum = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        decimal AcumDe(string id)
        {
            if (memoAcum.TryGetValue(id, out var c)) return c;
            if (!porId.TryGetValue(id, out var nodo) || !enProcAcum.Add(id)) return 0m;
            decimal acum;
            if (!EsNumerico(nodo.LineItemId) && !string.IsNullOrWhiteSpace(nodo.Referencia))
                acum = EvaluarFormula(nodo.Referencia, refId => AcumDe(refId), todosIds);
            else
            {
                var v = ValoresDe(id);
                acum = 0m; for (int m = 0; m < 12; m++) acum += v[m];
            }
            enProcAcum.Remove(id);
            memoAcum[id] = acum;
            return acum;
        }

        // ── Construir nodos de respuesta ─────────────────────────────────────
        var nodos = cuentas.Select(c =>
        {
            var esHoja  = EsNumerico(c.LineItemId);
            var tieneHijos = hijosDe.ContainsKey(c.LineItemId);
            var vals = ValoresDe(c.LineItemId);
            var acum = AcumDe(c.LineItemId);

            // Descuadre: solo para nodos con hijos (valor propio vs suma de hijos)
            decimal descuadre = 0m;
            if (tieneHijos && hijosDe.TryGetValue(c.LineItemId, out var hs))
                descuadre = acum - hs.Sum(h => AcumDe(h.LineItemId));

            return new PnlNodoDto
            {
                LineItemId     = c.LineItemId,
                AccountName    = c.AccountName,
                Etiqueta       = esHoja && tieneHijos == false
                                    ? $"{c.LineItemId} {c.AccountName}".Trim()  // último nivel: "LineItemId Account Name"
                                    : c.AccountName,                             // agrupador: solo Account Name
                ParentId       = c.ParentId,
                Nivel          = c.Nivel,
                TipoFinanciero = c.TipoFinanciero,
                TieneFormula   = !string.IsNullOrWhiteSpace(c.Referencia),
                TieneHijos     = tieneHijos,
                EsHoja         = esHoja,
                SinMovimientos = esHoja && !cuentasConMov.Contains(c.LineItemId),
                Valores        = vals.Select(v => Math.Round(v, 2)).ToArray(),
                Acum           = Math.Round(acum, 2),
                Descuadre      = Math.Round(descuadre, 2),
                Orden          = c.Orden,
            };
        }).ToList();

        return ApiResponse<PnlResponseDto>.Ok(new PnlResponseDto
        {
            Año    = año,
            Moneda = moneda,
            Nodos  = nodos,
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Evaluador de fórmulas: sustituye cada LineItemId por su valor y evalúa
    // la aritmética (+, −, *, /, paréntesis). Los ids se reemplazan más largos
    // primero para no confundir sufijos (EB vs EBM); el guion dentro del id no
    // se toma como resta gracias a los límites (?<![\w-]) … (?![\w-]).
    // ════════════════════════════════════════════════════════════════════════
    private static decimal EvaluarFormula(string formula, Func<string, decimal> valorDe, IEnumerable<string> idsConocidos)
    {
        var expr = formula;
        foreach (var id in idsConocidos.Where(i => !EsNumerico(i)).OrderByDescending(i => i.Length))
        {
            if (expr.IndexOf(id, StringComparison.OrdinalIgnoreCase) < 0) continue;
            var val = valorDe(id).ToString(CultureInfo.InvariantCulture);
            expr = Regex.Replace(expr,
                $@"(?<![A-Za-z0-9\-]){Regex.Escape(id)}(?![A-Za-z0-9\-])",
                "(" + val + ")", RegexOptions.IgnoreCase);
        }
        try { return new Aritmetica(expr).Evaluar(); }
        catch { return 0m; }
    }

    // Evaluador aritmético recursivo-descendente (+ − * / y paréntesis).
    private sealed class Aritmetica
    {
        private readonly string _s;
        private int _pos;
        public Aritmetica(string s) { _s = s; _pos = 0; }

        public decimal Evaluar()
        {
            var v = ParseExpr();
            return v;
        }
        private decimal ParseExpr()
        {
            var v = ParseTerm();
            while (true)
            {
                SkipWs();
                if (Match('+')) v += ParseTerm();
                else if (Match('-')) v -= ParseTerm();
                else break;
            }
            return v;
        }
        private decimal ParseTerm()
        {
            var v = ParseFactor();
            while (true)
            {
                SkipWs();
                if (Match('*')) v *= ParseFactor();
                else if (Match('/')) { var d = ParseFactor(); v = d == 0 ? 0 : v / d; }
                else break;
            }
            return v;
        }
        private decimal ParseFactor()
        {
            SkipWs();
            if (Match('(')) { var v = ParseExpr(); SkipWs(); Match(')'); return v; }
            if (Match('-')) return -ParseFactor();
            if (Match('+')) return ParseFactor();
            return ParseNumber();
        }
        private decimal ParseNumber()
        {
            SkipWs();
            int start = _pos;
            while (_pos < _s.Length && (char.IsDigit(_s[_pos]) || _s[_pos] == '.')) _pos++;
            var tok = _s[start.._pos];
            return decimal.TryParse(tok, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }
        private void SkipWs() { while (_pos < _s.Length && char.IsWhiteSpace(_s[_pos])) _pos++; }
        private bool Match(char c) { SkipWs(); if (_pos < _s.Length && _s[_pos] == c) { _pos++; return true; } return false; }
    }

    // ════════════════════════════════════════════════════════════════════════
    // GET /api/pyl/filtros — Cliente/Proyecto/Vertical + años de GR55 (HUE-07)
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<PnlFiltrosDto>> ObtenerFiltrosAsync(PnlFiltros f)
    {
        var ultimoId = await ResolverConsolidacionAsync(f.ConsolidacionId);
        if (ultimoId is null)
            return ApiResponse<PnlFiltrosDto>.Ok(new PnlFiltrosDto(), "Sin consolidación disponible.");

        var baseQ = _db.MovimientosGR55.AsNoTracking().Where(m => m.ConsolidacionId == ultimoId);

        var clientes  = await baseQ.Select(m => m.Cliente).Where(v => v != "").Distinct().OrderBy(v => v).ToListAsync();
        var proyectos = await baseQ.Select(m => m.CodProyecto).Where(v => v != "").Distinct().OrderBy(v => v).ToListAsync();
        var verticales = await baseQ.Select(m => m.Vertical).Where(v => v != "").Distinct().OrderBy(v => v).ToListAsync();
        var años      = await baseQ.Select(m => m.Año).Distinct().OrderByDescending(v => v).ToListAsync();

        return ApiResponse<PnlFiltrosDto>.Ok(new PnlFiltrosDto
        {
            Clientes   = clientes,
            Proyectos  = proyectos,
            Verticales = verticales,
            Años       = años,
        });
    }
}
