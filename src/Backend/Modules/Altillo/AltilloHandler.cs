using System.Text.Json;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.Altillo
{
    public class AltilloHandler
    {
        private readonly AltilloService _service;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public AltilloHandler(DbService db)
        {
            _service = new AltilloService(db);
        }

        // =========================
        // ENTRY POINT
        // =========================
        public async Task<string> Handle(string action, Dictionary<string, object>? data)
        {
            try
            {
                Console.WriteLine($"📥 ALTILLO ACTION: {action}");

                return action switch
{
    "altillo.obtenerRegistros" => await ObtenerRegistros(data),
    "altillo.guardarCambios" => await GuardarCambios(data),
    "altillo.exportarExcel" => await ExportarExcel(data),
    "altillo.obtenerKpis" => await ObtenerKpis(), // 🔥 NUEVO
    _ => Error("Acción no válida")
};
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR ALTILLO: {ex}");

                return Error(ex.Message);
            }
        }

        // =========================
        // HELPERS
        // =========================
        private string? GetString(Dictionary<string, object>? data, string key)
        {
            if (data == null || !data.ContainsKey(key)) return null;

            var val = data[key];

            if (val is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.Null ? null : je.ToString();
            }

            return val?.ToString();
        }

        private int GetInt(Dictionary<string, object>? data, string key, int defaultValue)
        {
            if (data == null || !data.ContainsKey(key)) return defaultValue;

            var val = data[key];

            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out int num))
                    return num;

                if (je.ValueKind == JsonValueKind.String && int.TryParse(je.GetString(), out int numStr))
                    return numStr;
            }

            try
            {
                return Convert.ToInt32(val);
            }
            catch
            {
                return defaultValue;
            }
        }

        // =========================
        // OBTENER REGISTROS
        // =========================
        private Task<string> ObtenerRegistros(Dictionary<string, object>? data)
        {
            string? fechaDesde = GetString(data, "fechaDesde");
            string? fechaHasta = GetString(data, "fechaHasta");
            string? codigo = GetString(data, "codigo");
            string? lote = GetString(data, "lote");

            int page = GetInt(data, "page", 1);
            int limit = GetInt(data, "limit", 20);

            var (registros, total) = _service.ObtenerRegistros(
                fechaDesde,
                fechaHasta,
                codigo,
                lote,
                page,
                limit
            );

            int totalPages = (int)Math.Ceiling((double)total / limit);

            return Task.FromResult(Ok(new
            {
                data = registros,
                total = total,
                page = page,
                pages = totalPages
            }));
        }

        // =========================
        // GUARDAR CAMBIOS
        // =========================
        private Task<string> GuardarCambios(Dictionary<string, object>? data)
        {
            if (data == null || !data.ContainsKey("data"))
            {
                return Task.FromResult(Error("No se recibieron cambios"));
            }

            List<AltilloCambioItem>? cambios;

            try
            {
                var json = JsonSerializer.Serialize(data["data"]);
                cambios = JsonSerializer.Deserialize<List<AltilloCambioItem>>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ERROR DESERIALIZANDO CAMBIOS: " + ex.Message);

                return Task.FromResult(Error("Error procesando cambios"));
            }

            if (cambios == null || cambios.Count == 0)
            {
                return Task.FromResult(Error("Lista de cambios vacía"));
            }

            Console.WriteLine($"🧠 Cambios recibidos: {cambios.Count}");

            try
            {
                _service.GuardarCambios(cambios);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ERROR GUARDANDO: " + ex.Message);
                return Task.FromResult(Error("Error al guardar en base de datos"));
            }

            return Task.FromResult(Ok(new { guardados = cambios.Count }));
        }

        // =========================
        // EXPORTAR EXCEL
        // =========================
        private Task<string> ExportarExcel(Dictionary<string, object>? data)
        {
            string? fechaDesde = GetString(data, "fechaDesde");
            string? fechaHasta = GetString(data, "fechaHasta");
            string? codigo = GetString(data, "codigo");
            string? lote = GetString(data, "lote");

            try
            {
                var path = _service.ExportarExcel(
                    fechaDesde,
                    fechaHasta,
                    codigo,
                    lote
                );

                return Task.FromResult(Ok(new { path }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ERROR EXPORTANDO: " + ex.Message);
                return Task.FromResult(Error("Error al exportar Excel"));
            }
        }
        // =========================
// KPIS
// =========================
private Task<string> ObtenerKpis()
{
    try
    {
        var kpis = _service.ObtenerKpis();

        return Task.FromResult(Ok(kpis));
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ ERROR KPIS: " + ex.Message);
        return Task.FromResult(Error("Error obteniendo KPIs"));
    }
}

        // =========================
        // RESPUESTAS ESTÁNDAR
        // =========================
        private string Ok(object? data)
        {
            return JsonSerializer.Serialize(new
            {
                ok = true,
                data,
                error = (string?)null
            }, _jsonOptions);
        }

        private string Error(string message)
        {
            return JsonSerializer.Serialize(new
            {
                ok = false,
                data = (object?)null,
                error = message
            }, _jsonOptions);
        }
    }
}