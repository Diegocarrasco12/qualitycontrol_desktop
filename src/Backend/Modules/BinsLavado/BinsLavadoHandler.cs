using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LogisticControlCenter.Services;
using System.IO;
using System.Diagnostics;

namespace LogisticControlCenter.Modules.BinsLavado
{
    public class BinsLavadoHandler
    {
        private readonly BinsLavadoService _service;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public BinsLavadoHandler(DbService db)
        {
            _service = new BinsLavadoService(db);
        }

        // =========================
        // ENTRY POINT
        // =========================
        public async Task<string> Handle(string action, Dictionary<string, object>? data)
        {
            try
            {
                Console.WriteLine($"📥 BINS LAVADO ACTION: {action}");

                return action switch
                {
                    "binsLavado.obtener" => await Obtener(ExtractData(data)),
                    "binsLavado.obtenerKPI" => await ObtenerKPI(),
                    "binsLavado.exportarExcel" => await ExportarExcel(ExtractData(data)),
                    "binsLavado.abrirArchivo" => AbrirArchivo(data),
                    _ => JsonError($"Acción no válida: {action}")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR HANDLER LAVADO: {ex}");
                return JsonError(ex.Message);
            }
        }

        // =========================
        // OBTENER REGISTROS
        // =========================
        private async Task<string> Obtener(Dictionary<string, object>? data)
        {
            try
            {
                int page = GetInt(data, "page", 1);
                int limit = GetInt(data, "limit", 20);

                string fechaDesde = GetString(data, "fechaDesde");
                string fechaHasta = GetString(data, "fechaHasta");
                string bin = GetString(data, "bin");
                string calle = GetString(data, "calle");
                string documento = GetString(data, "documento");

                var result = _service.ObtenerLavados(
                    page,
                    limit,
                    fechaDesde,
                    fechaHasta,
                    bin,
                    calle,
                    documento
                );

                return JsonOk(new
                {
                    items = result.items,
                    total = result.total,
                    pages = result.pages
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR ObtenerLavados: {ex}");
                return JsonError(ex.Message);
            }
        }

        // =========================
        // KPI
        // =========================
        private async Task<string> ObtenerKPI()
        {
            try
            {
                var totalHoy = _service.ObtenerKPIHoy();

                return JsonOk(new
                {
                    totalHoy = totalHoy
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR KPI LAVADO: {ex}");
                return JsonError(ex.Message);
            }
        }

        // =========================
        // EXPORTAR EXCEL
        // =========================
        private async Task<string> ExportarExcel(Dictionary<string, object>? data)
{
    try
    {
        string fechaDesde = GetString(data, "fechaDesde");
        string fechaHasta = GetString(data, "fechaHasta");
        string bin = GetString(data, "bin");
        string calle = GetString(data, "calle");
        string documento = GetString(data, "documento");

        var path = await _service.ExportarExcel(
            fechaDesde,
            fechaHasta,
            bin,
            calle,
            documento
        );

        return JsonOk(new { path });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR ExportarExcel: {ex}");
        return JsonError(ex.Message);
    }
}

        // =========================
        // ABRIR ARCHIVO
        // =========================
        private string AbrirArchivo(Dictionary<string, object>? data)
        {
            try
            {
                var path = GetString(data, "path");

                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Ruta vacía");

                var cleanPath = path.StartsWith("/") ? path : "/" + path;

                var url = $"https://consumo_papel.faret.cl{cleanPath}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                return JsonOk(new { url });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR AbrirArchivo: {ex}");
                return JsonError(ex.Message);
            }
        }

        // =========================
        // EXTRACT DATA
        // =========================
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

        // =========================
        // HELPERS
        // =========================
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

        // =========================
        // RESPUESTAS
        // =========================
        private string JsonOk(object data)
        {
            return JsonSerializer.Serialize(new
            {
                ok = true,
                data
            }, _jsonOptions);
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