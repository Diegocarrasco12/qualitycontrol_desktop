using System.Diagnostics;
using System.Text.Json;
using ClosedXML.Excel;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.ConsumoPapel
{
    public class ConsumoPapelHandler
    {
        private readonly ConsumoPapelService _service;

        public ConsumoPapelHandler(DbService db)
        {
            _service = new ConsumoPapelService(db);
        }

        public async Task<string> Handle(string action, Dictionary<string, object>? payload)
        {
            try
            {
                Console.WriteLine($"📥 ACTION: {action}");

                switch (action)
                {
                    case "consumo.obtenerConsumos":
                        return await ObtenerConsumos(ExtractData(payload));

                    case "consumo.obtenerKpis":
                        return await ObtenerKpis();

                    case "consumo.guardarCambios":
                        return await GuardarCambios(payload); // 🔥 directo

                    case "consumo.exportarExcel":
                        return await ExportarExcel(ExtractData(payload));

                    default:
                        return Error($"Acción no reconocida: {action}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR HANDLER: {ex}");

                return Error(ex.Message); // 🔥 ESTE ES EL CAMBIO
            }
        }

        private async Task<string> ObtenerConsumos(Dictionary<string, object>? data)
        {
            int page = GetInt(data, "page", 1);
            int limit = GetInt(data, "limit", 20);

            string fechaDesde = GetString(data, "fechaDesde");
            string fechaHasta = GetString(data, "fechaHasta");
            string codigo = GetString(data, "codigo");
            string lote = GetString(data, "lote");

            var result = await _service.ObtenerConsumos(
                page,
                limit,
                fechaDesde,
                fechaHasta,
                codigo,
                lote
            );

            return Ok(
                new
                {
                    items = result.Items ?? new List<ConsumoPapelItem>(),
                    page,
                    pages = result.Pages,
                    total = result.Total,
                }
            );
        }

        private async Task<string> ObtenerKpis()
        {
            var kpis = await _service.ObtenerKpis();

            return Ok(
                new
                {
                    consumoHoy = kpis.ConsumoHoy,
                    tarjasHoy = kpis.TarjasHoy,
                    saldoTotal = kpis.SaldoTotal,
                    ultimoCodigo = kpis.UltimoCodigo,
                    ultimoLote = kpis.UltimoLote,
                }
            );
        }

        private async Task<string> GuardarCambios(Dictionary<string, object>? payload)
        {
            var cambios = ParseCambios(payload);

            if (cambios.Count == 0)
                return Error("No hay cambios válidos");

            await _service.GuardarCambios(cambios);

            return Ok(new { guardados = cambios.Count });
        }

        private async Task<string> ExportarExcel(Dictionary<string, object>? data)
        {
            int limit = 100000;

            string fechaDesde = GetString(data, "fechaDesde");
            string fechaHasta = GetString(data, "fechaHasta");
            string codigo = GetString(data, "codigo");
            string lote = GetString(data, "lote");

            var result = await _service.ObtenerConsumos(
                1,
                limit,
                fechaDesde,
                fechaHasta,
                codigo,
                lote
            );

            if (result.Items == null || result.Items.Count == 0)
                return Error("No hay datos para exportar");

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktop, $"consumos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Consumos");

                string[] headers =
                {
                    "ID",
                    "Fecha",
                    "Descripción",
                    "Código",
                    "ConsumoKg",
                    "NP",
                    "TarjaKg",
                    "SaldoKg",
                   "Lote",
"Ubicación SAP",
"Estado",
"Salida",
                };

                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(1, i + 1).Value = headers[i];

                int row = 2;

                foreach (var item in result.Items)
                {
                    ws.Cell(row, 1).Value = item.Id;
                    ws.Cell(row, 2).Value = item.Fecha;
                    ws.Cell(row, 3).Value = item.Descripcion;
                    ws.Cell(row, 4).Value = item.Codigo;
                    ws.Cell(row, 5).Value = item.ConsumoKg;
                    ws.Cell(row, 6).Value = item.NP;
                    ws.Cell(row, 7).Value = item.TarjaKg;
                    ws.Cell(row, 8).Value = item.SaldoKg;
                    ws.Cell(row, 9).Value = item.Lote;
ws.Cell(row, 10).Value = item.UbicacionBin;
ws.Cell(row, 11).Value = item.Estado;
ws.Cell(row, 12).Value = item.Salida;
                    row++;
                }

                workbook.SaveAs(filePath);
                Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            }

            return Ok(new { path = filePath });
        }

        // 🔥 SOLO PARA FILTROS (OBJETOS)
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

        private string Ok(object? data)
        {
            return JsonSerializer.Serialize(
                new
                {
                    ok = true,
                    data,
                    error = (string?)null,
                }
            );
        }

        private string Error(string message)
        {
            return JsonSerializer.Serialize(
                new
                {
                    ok = false,
                    data = (object?)null,
                    error = message,
                }
            );
        }

        private static string GetString(Dictionary<string, object>? data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
                return "";

            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String ? je.GetString() ?? "" : je.ToString();
            }

            return value.ToString() ?? "";
        }

        private static int GetInt(Dictionary<string, object>? data, string key, int defaultValue)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
                return defaultValue;

            if (value is JsonElement je)
            {
                if (je.TryGetInt32(out var n))
                    return n;

                if (int.TryParse(je.ToString(), out var ns))
                    return ns;
            }

            return int.TryParse(value.ToString(), out var result) ? result : defaultValue;
        }

        // 🔥 PARSE SIMPLE Y CORRECTO
        private static List<ConsumoCambioItem> ParseCambios(Dictionary<string, object>? payload)
        {
            var result = new List<ConsumoCambioItem>();

            if (payload == null || !payload.TryGetValue("data", out var raw))
                return result;

            if (raw is not JsonElement je || je.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in je.EnumerateArray())
            {
                try
                {
                    result.Add(
                        new ConsumoCambioItem
                        {
                            Id = item.GetProperty("id").GetInt32(),
                            Estado = item.GetProperty("estado").GetString() ?? "",
                            Salida = item.GetProperty("salida").GetString() ?? "",
                        }
                    );
                }
                catch { }
            }

            Console.WriteLine($"📦 CAMBIOS RECIBIDOS: {result.Count}");

            return result;
        }
    }
}
