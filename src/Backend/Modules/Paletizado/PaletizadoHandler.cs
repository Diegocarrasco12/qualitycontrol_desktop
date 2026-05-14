using System.Text.Json;
using ClosedXML.Excel;
using LogisticControlCenter.Repositories.Paletizado;
using LogisticControlCenter.Services;
using MySqlConnector;

namespace LogisticControlCenter.Modules.Paletizado
{
    public class PaletizadoHandler
    {
        private readonly PaletizadoService _service;
        private readonly DbService _db;

        public PaletizadoHandler(DbService db)
        {
            _db = db;
            var repo = new PaletizadoRepository(db);
            _service = new PaletizadoService(repo);
        }

        public async Task<string> Handle(string action, Dictionary<string, object>? payload)
        {
            try
            {
                Console.WriteLine($"📥 ACTION: {action}");

                switch (action)
                {
                    case "paletizado.obtenerPalets":
                        return await ObtenerPalets(ExtractData(payload));

                    case "paletizado.obtenerKPI":
                        return await ObtenerKPI();

                    case "paletizado.exportarExcel":
                        return await ExportarExcel(ExtractData(payload));

                    default:
                        return Error($"Acción no reconocida: {action}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR HANDLER: {ex}");
                return Error(ex.Message);
            }
        }

        // =========================
        // LISTADO
        // =========================
        private async Task<string> ObtenerPalets(Dictionary<string, object>? data)
        {
            int page = GetInt(data, "page", 1);
            int limit = GetInt(data, "limit", 20);

            string fechaDesde = GetString(data, "fechaDesde");
            string fechaHasta = GetString(data, "fechaHasta");
            string planta = GetString(data, "planta");
            string taller = GetString(data, "taller");
            string np = GetString(data, "np");
            string idPalet = GetString(data, "idPalet");

            var result = await _service.ObtenerPalets(
                page,
                limit,
                fechaDesde,
                fechaHasta,
                planta,
                taller,
                np,
                idPalet
            );

            return Ok(result);
        }

        private async Task<string> ObtenerKPI()
        {
            try
            {
                using var conn = _db.GetRegistroPaletizadoConnection();
                await conn.OpenAsync();

                string sql = "SELECT COUNT(*) FROM palets";

                using var cmd = new MySqlCommand(sql, conn);
                var total = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return Ok(new { total });
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        private async Task<string> ExportarExcel(Dictionary<string, object>? data)
        {
            try
            {
                string fechaDesde = GetString(data, "fechaDesde");
                string fechaHasta = GetString(data, "fechaHasta");
                string planta = GetString(data, "planta");
                string taller = GetString(data, "taller");
                string np = GetString(data, "np");
                string idPalet = GetString(data, "idPalet");

                using var conn = _db.GetRegistroPaletizadoConnection();
                await conn.OpenAsync();

                var where = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(fechaDesde) && !string.IsNullOrEmpty(fechaHasta))
                {
                    where.Add("p.fecha_registro BETWEEN @desde AND @hasta");
                    parameters.Add(new MySqlParameter("@desde", fechaDesde + " 00:00:00"));
                    parameters.Add(new MySqlParameter("@hasta", fechaHasta + " 23:59:59"));
                }

                if (!string.IsNullOrEmpty(planta))
                {
                    where.Add("p.planta_produccion = @planta");
                    parameters.Add(new MySqlParameter("@planta", planta));
                }

                if (!string.IsNullOrEmpty(taller))
                {
                    where.Add("p.taller_paletizado = @taller");
                    parameters.Add(new MySqlParameter("@taller", taller));
                }

                if (!string.IsNullOrEmpty(np))
                {
                    where.Add("p.np_cliente LIKE @np");
                    parameters.Add(new MySqlParameter("@np", $"%{np}%"));
                }

                if (!string.IsNullOrEmpty(idPalet))
                {
                    where.Add("p.id_palet LIKE @idPalet");
                    parameters.Add(new MySqlParameter("@idPalet", $"%{idPalet}%"));
                }

                string whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

                string sql =
                    $@"
            SELECT 
                p.id_palet,
                p.fecha_registro,
                p.planta_produccion,
                p.np_cliente,
                p.nota_venta_innpack,
                p.nombre_cliente,
                p.taller_paletizado,
                p.tipo_palet,
                p.cantidad,
                COALESCE(p.descripcion_sap, p.descripcion) AS descripcion,
                p.fecha_impresion,
                p.unidades_por_pliego,
                p.valor_unitario,
                p.valor_total_palet,
                u.nombre_completo AS emisor_tarja
            FROM palets p
            LEFT JOIN etiquetas_temp et ON et.correlativo = p.id_palet
            LEFT JOIN usuarios u ON u.id = et.created_by_user_id
            {whereSql}
            ORDER BY p.fecha_registro DESC;
        ";

                using var cmd = new MySqlCommand(sql, conn);

                foreach (var p in parameters)
                    cmd.Parameters.Add(p);

                using var reader = await cmd.ExecuteReaderAsync();

                var fileName = $"palets_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    fileName
                );

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Palets");

                string[] headers =
                {
                    "ID Palet",
                    "Fecha",
                    "Planta",
                    "NP Cliente",
                    "NP Innpack",
                    "Nombre Cliente",
                    "Taller",
                    "Tipo",
                    "Cantidad",
                    "Descripción",
                    "Fecha de Impresión",
                    "Unid. x Pliego",
                    "Valor Unitario",
                    "Valor Total",
                    "Emisor de Tarja",
                };

                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(1, i + 1).Value = headers[i];

                int row = 2;
                decimal totalGeneral = 0;

                while (await reader.ReadAsync())
                {
                    decimal valorUnitario =
                        reader["valor_unitario"] != DBNull.Value
                            ? Convert.ToDecimal(reader["valor_unitario"])
                            : 0;
                    decimal valorTotal =
                        reader["valor_total_palet"] != DBNull.Value
                            ? Convert.ToDecimal(reader["valor_total_palet"])
                            : 0;

                    totalGeneral += valorTotal;

                    ws.Cell(row, 1).Value = reader["id_palet"]?.ToString();
                    ws.Cell(row, 2).Value =
                        reader["fecha_registro"] != DBNull.Value
                            ? Convert
                                .ToDateTime(reader["fecha_registro"])
                                .ToString("dd-MM-yyyy HH:mm")
                            : "";
                    ws.Cell(row, 3).Value = reader["planta_produccion"]?.ToString();
                    ws.Cell(row, 4).Value = reader["np_cliente"]?.ToString();
                    ws.Cell(row, 5).Value = reader["nota_venta_innpack"]?.ToString();
                    ws.Cell(row, 6).Value = reader["nombre_cliente"]?.ToString();
                    ws.Cell(row, 7).Value = reader["taller_paletizado"]?.ToString();
                    ws.Cell(row, 8).Value = reader["tipo_palet"]?.ToString();
                    ws.Cell(row, 9).Value = reader["cantidad"]?.ToString();
                    ws.Cell(row, 10).Value = reader["descripcion"]?.ToString();
                    ws.Cell(row, 11).Value =
                        reader["fecha_impresion"] != DBNull.Value
                            ? Convert.ToDateTime(reader["fecha_impresion"]).ToString("dd-MM-yyyy")
                            : "";
                    ws.Cell(row, 12).Value = reader["unidades_por_pliego"]?.ToString();
                    ws.Cell(row, 13).Value = valorUnitario;
                    ws.Cell(row, 14).Value = valorTotal;
                    ws.Cell(row, 15).Value = reader["emisor_tarja"]?.ToString();

                    row++;
                }

                ws.Cell(row, 13).Value = "TOTAL";
                ws.Cell(row, 14).Value = totalGeneral;

                ws.Range(1, 1, 1, 15).Style.Font.Bold = true;
                ws.Range(1, 1, 1, 15).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

                ws.Column(13).Style.NumberFormat.Format = "$#,##0.00";
                ws.Column(14).Style.NumberFormat.Format = "$#,##0.00";

                ws.Columns().AdjustToContents();

                wb.SaveAs(path);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true,
                    }
                );

                return Ok(new { path });
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        // =========================
        // UTILS (MISMO PATRÓN)
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
    }
}
