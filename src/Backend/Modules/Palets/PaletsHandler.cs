using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.Palets
{
    public class PaletsHandler
    {
        private readonly PaletsService _service;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public PaletsHandler(DbService db)
        {
            _service = new PaletsService(db);
        }

        public async Task<string> Handle(string action, Dictionary<string, object>? data)
        {
            try
            {
                Console.WriteLine($"📥 PALETS ACTION: {action}");

                return action switch
                {
                    "palets.obtenerRegistros" => await ObtenerRegistros(ExtractData(data)),
                    "palets.obtenerKPI" => await ObtenerKPI(),
                    "palets.exportarExcel" => await ExportarExcel(ExtractData(data)),                    _ => JsonError($"Acción no reconocida: {action}")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR PALETS: {ex}");
                return JsonError(ex.Message);
            }
        }

        private Task<string> ObtenerRegistros(Dictionary<string, object>? data)
        {
            int page = GetInt(data, "page", 1);
            int limit = GetInt(data, "limit", 20);

            string fechaDesde = GetString(data, "fechaDesde");
            string fechaHasta = GetString(data, "fechaHasta");
            string lote = GetString(data, "lote");
            string planta = GetString(data, "planta");
            string tipo = GetString(data, "tipo");

            Console.WriteLine($"📊 PALETS FILTROS => page:{page} limit:{limit} fechaDesde:{fechaDesde} fechaHasta:{fechaHasta} lote:{lote} planta:{planta} tipo:{tipo}");

            var result = _service.ObtenerRegistros(
                page,
                limit,
                fechaDesde,
                fechaHasta,
                lote,
                planta,
                tipo
            );

            return Task.FromResult(JsonSerializer.Serialize(new
            {
                ok = true,
                data = new
                {
                    items = result.items,
                    total = result.total,
                    pages = result.pages
                }
            }, _jsonOptions));
        }

        private Task<string> ObtenerKPI()
        {
            var hoy = _service.ObtenerKPIHoy();
            var ultimo = _service.ObtenerKPIUltimoDia();

            return Task.FromResult(JsonSerializer.Serialize(new
            {
                ok = true,
                data = new
                {
                    totalHoy = hoy,
                    totalUltimoDia = ultimo
                }
            }, _jsonOptions));
        }

        private async Task<string> ExportarExcel(Dictionary<string, object>? data)
{
    try
    {
        string fechaDesde = GetString(data, "fechaDesde");
        string fechaHasta = GetString(data, "fechaHasta");
        string lote = GetString(data, "lote");
        string planta = GetString(data, "planta");
        string tipo = GetString(data, "tipo");

        Console.WriteLine($"📤 EXPORT PALETS => desde:{fechaDesde} hasta:{fechaHasta} lote:{lote} planta:{planta} tipo:{tipo}");

        var path = await _service.ExportarExcel(
            fechaDesde,
            fechaHasta,
            lote,
            planta,
            tipo
        );

        return JsonSerializer.Serialize(new
        {
            ok = true,
            path
        }, _jsonOptions);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR EXPORT PALETS: {ex}");
        return JsonError(ex.Message);
    }
}

        private static Dictionary<string, object>? ExtractData(Dictionary<string, object>? payload)
        {
            if (payload == null || !payload.ContainsKey("data"))
                return null;

            if (payload["data"] is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(je.GetRawText());
            }

            return null;
        }

        private int GetInt(Dictionary<string, object>? data, string key, int def = 0)
        {
            if (data == null || !data.ContainsKey(key) || data[key] == null)
                return def;

            if (data[key] is JsonElement je)
            {
                if (je.TryGetInt32(out var n))
                    return n;

                if (int.TryParse(je.ToString(), out var ns))
                    return ns;
            }

            return int.TryParse(data[key].ToString(), out var result) ? result : def;
        }

        private string GetString(Dictionary<string, object>? data, string key)
        {
            if (data == null || !data.ContainsKey(key) || data[key] == null)
                return "";

            if (data[key] is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String
                    ? je.GetString() ?? ""
                    : je.ToString();
            }

            return data[key].ToString() ?? "";
        }

        private string JsonError(string message)
        {
            return JsonSerializer.Serialize(new
            {
                ok = false,
                error = message
            }, _jsonOptions);
        }
    }
}