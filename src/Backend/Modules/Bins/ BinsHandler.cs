using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LogisticControlCenter.Services;
using System.IO;
using System.Diagnostics;

namespace LogisticControlCenter.Modules.Bins
{
    public class BinsHandler
    {
        private readonly BinsService _service;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public BinsHandler(DbService db)
        {
            _service = new BinsService(db);
        }

        // =========================
        // ENTRY POINT
        // =========================
        public async Task<string> Handle(string action, Dictionary<string, object>? data)
        {
            try
            {
                Console.WriteLine($"📥 BINS ACTION: {action}");

                return action switch
                {
                    "bins.obtenerRegistros" => await ObtenerRegistros(ExtractData(data)),
                    "bins.guardarCambios" => await GuardarCambios(data),
                    "bins.abrirArchivo" => AbrirArchivo(data),
                    "bins.obtenerKPI" => await ObtenerKPI(),
                    "bins.exportarExcel" => await ExportarExcel(data),
                    _ => JsonError($"Acción no reconocida: {action}")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR BINS: {ex}");
                return JsonError(ex.Message);
            }
        }

        // =========================
        // OBTENER REGISTROS
        // =========================
        private async Task<string> ObtenerRegistros(Dictionary<string, object>? data)
        {
            try
            {
                int page = GetInt(data, "page", 1);
                int limit = GetInt(data, "limit", 20);

                string fechaDesde = GetString(data, "fechaDesde");
                string fechaHasta = GetString(data, "fechaHasta");
                string bin = GetString(data, "bin");
                string calle = GetString(data, "calle");
                string tipo = GetString(data, "tipo");
                string documento = GetString(data, "documento");

                Console.WriteLine($"📊 PAGE: {page} | BIN: {bin} | TIPO: {tipo}");

                var result = await _service.ObtenerRegistros(
                    page,
                    limit,
                    fechaDesde,
                    fechaHasta,
                    bin,
                    calle,
                    tipo,
                    documento
                );

                return JsonOk(new
                {
                    items = result.Items,
                    total = result.Total,
                    pages = result.Pages
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR ObtenerRegistros: {ex}");
                return JsonError(ex.Message);
            }
        }
        // =========================
// KPI HOY
// =========================
private async Task<string> ObtenerKPI()
{
    try
    {
        var (entradas, salidas) = await _service.ObtenerKPIHoy();

        return JsonOk(new
        {
            entradasHoy = entradas,
            salidasHoy = salidas
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR KPI: {ex}");
        return JsonError(ex.Message);
    }
}

        // =========================
        // GUARDAR CAMBIOS
        // =========================
        private async Task<string> GuardarCambios(Dictionary<string, object>? data)
        {
            try
            {
                if (data == null || !data.ContainsKey("cambios"))
                    throw new Exception("No se recibieron cambios");

                var raw = data["cambios"];

                var cambios = JsonSerializer.Deserialize<List<BinsCambioItem>>(
                    JsonSerializer.Serialize(raw),
                    _jsonOptions
                ) ?? new List<BinsCambioItem>();

                await _service.GuardarCambios(cambios);

                return JsonOk(new
                {
                    message = "Cambios guardados correctamente"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR GuardarCambios: {ex}");
                return JsonError(ex.Message);
            }
        }

        // =========================
        // 🔥 ABRIR ARCHIVO (FINAL PRO)
        // =========================
        private string AbrirArchivo(Dictionary<string, object>? data)
        {
            try
            {
                var path = GetString(data, "path");

                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Ruta vacía");

                // Normalizar path
                var cleanPath = path.StartsWith("/") ? path : "/" + path;

                // URL correcta (HTTPS)
                var url = $"https://consumo_papel.faret.cl{cleanPath}";

                Console.WriteLine($"🌐 URL FINAL: {url}");

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                return JsonOk(new
                {
                    ok = true,
                    url = url,
                    message = "Archivo abierto en navegador"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR AbrirArchivo: {ex}");
                return JsonError($"Error abriendo archivo: {ex.Message}");
            }
        }
        // =========================
// EXPORTAR EXCEL
// =========================
private async Task<string> ExportarExcel(Dictionary<string, object>? data)
{
    try
    {
        var filtros = data;

        string fechaDesde = GetString(filtros, "fechaDesde");
        string fechaHasta = GetString(filtros, "fechaHasta");
        string bin = GetString(filtros, "bin");
        string calle = GetString(filtros, "calle");
        string tipo = GetString(filtros, "tipo");
        string documento = GetString(filtros, "documento");

        // 🔥 llamar al repo (igual que consumo)
        var path = await _service.ExportarExcel(
            fechaDesde,
            fechaHasta,
            bin,
            calle,
            tipo,
            documento
        );

        Console.WriteLine($"📁 Excel generado en: {path}");

        // 🔥 abrir automáticamente
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });

        return JsonOk(new { path });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR ExportarExcel: {ex}");
        return JsonError(ex.Message);
    }
}

        // =========================
        // 🔥 EXTRACT DATA (ÚNICO)
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