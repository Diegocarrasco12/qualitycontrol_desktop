using System;
using System.Collections.Generic;
using MySqlConnector;
using LogisticControlCenter.Modules.Palets;
using LogisticControlCenter.Services;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;
using System.IO;

namespace LogisticControlCenter.Repositories.Palets
{
    public class PaletsRepository
    {
        private readonly DbService _db;

        public PaletsRepository(DbService db)
        {
            _db = db;
        }

        // =========================
        // OBTENER REGISTROS
        // =========================
        public (List<PaletsItem> items, int total, int pages) ObtenerRegistros(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string lote,
            string planta,
            string tipo
        )
        {
            var lista = new List<PaletsItem>();

            using var conn = _db.GetConsumoPapelConnection();
            conn.Open();

            var (where, parameters) = BuildWhere(fechaDesde, fechaHasta, lote, planta, tipo);

            // =========================
            // TOTAL
            // =========================
            int total = 0;

            var countSql = $@"
                SELECT COUNT(*) 
                FROM consumo_papel.palets_movimientos p
                {where}
            ";

            using (var countCmd = new MySqlCommand(countSql, conn))
            {
                foreach (var p in parameters)
                    countCmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));

                total = Convert.ToInt32(countCmd.ExecuteScalar());
            }

            int pages = (int)Math.Ceiling((double)total / limit);
            int offset = (page - 1) * limit;

            // =========================
            // DATA
            // =========================
            var sql = $@"
                SELECT 
                    p.id,
                    p.fecha,
                    p.planta,
                    p.tipo_movimiento,
                    p.ean13,
                    p.cantidad,
                    p.lote
                FROM consumo_papel.palets_movimientos p
                {where}
                ORDER BY p.fecha DESC
                LIMIT @limit OFFSET @offset
            ";

            using var cmd = new MySqlCommand(sql, conn);

            foreach (var param in parameters)
                cmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));

            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var ean = reader["ean13"]?.ToString() ?? "";

                lista.Add(new PaletsItem
                {
                    Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                    Fecha = reader["fecha"] != DBNull.Value
                        ? Convert.ToDateTime(reader["fecha"]).ToString("yyyy-MM-dd HH:mm:ss")
                        : "",
                    Planta = reader["planta"]?.ToString() ?? "",
                    TipoMovimiento = reader["tipo_movimiento"]?.ToString() ?? "",
                    Ean13 = ean,
                    Detalle = ObtenerDetalleSap(ean),
                    Cantidad = reader["cantidad"] != DBNull.Value ? Convert.ToInt32(reader["cantidad"]) : 0,
                    Lote = reader["lote"]?.ToString() ?? ""
                });
            }

            return (lista, total, pages);
        }

        // =========================
        // KPI HOY
        // =========================
        public int ObtenerKPIHoy()
        {
            using var conn = _db.GetConsumoPapelConnection();
            conn.Open();

            var sql = @"
                SELECT COUNT(*) 
                FROM consumo_papel.palets_movimientos
                WHERE DATE(fecha) = CURDATE()
            ";

            using var cmd = new MySqlCommand(sql, conn);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // =========================
        // KPI ÚLTIMO DÍA CON DATOS
        // =========================
        public int ObtenerKPIUltimoDia()
        {
            using var conn = _db.GetConsumoPapelConnection();
            conn.Open();

            var sql = @"
                SELECT COUNT(*) 
                FROM consumo_papel.palets_movimientos
                WHERE DATE(fecha) = (
                    SELECT DATE(MAX(fecha)) 
                    FROM consumo_papel.palets_movimientos
                )
            ";

            using var cmd = new MySqlCommand(sql, conn);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // =========================
        // BUILDER WHERE (FIX REAL)
        // =========================
        private (string where, List<MySqlParameter> parameters) BuildWhere(
            string fechaDesde,
            string fechaHasta,
            string lote,
            string planta,
            string tipo
        )
        {
            var where = "WHERE 1=1";
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(fechaDesde))
            {
                where += " AND p.fecha >= @desde";
                parameters.Add(new MySqlParameter("@desde", fechaDesde + " 00:00:00"));
            }

            if (!string.IsNullOrEmpty(fechaHasta))
            {
                where += " AND p.fecha <= @hasta";
                parameters.Add(new MySqlParameter("@hasta", fechaHasta + " 23:59:59"));
            }

            if (!string.IsNullOrEmpty(lote))
            {
                where += " AND p.lote LIKE @lote";
                parameters.Add(new MySqlParameter("@lote", "%" + lote + "%"));
            }

            if (!string.IsNullOrEmpty(planta))
            {
                where += " AND p.planta = @planta";
                parameters.Add(new MySqlParameter("@planta", planta));
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                where += " AND p.tipo_movimiento = @tipo";
                parameters.Add(new MySqlParameter("@tipo", tipo));
            }

            return (where, parameters);
        }

        // =========================
        // SAP HELPER
        // =========================
        private string ObtenerDetalleSap(string ean)
        {
            try
            {
                using var sap = _db.GetSapConnection();
                sap.Open();

                var sql = @"
                    SELECT TOP 1 ItemName
                    FROM ZZZProcesosProductivos
                    WHERE ItemCode = @ean
                ";

                using var cmd = new SqlCommand(sql, sap);
                cmd.Parameters.AddWithValue("@ean", ean);

                var result = cmd.ExecuteScalar();

                return result?.ToString() ?? "No encontrado";
            }
            catch
            {
                return "SAP error";
            }
        }

        // =========================
// EXPORTAR EXCEL (PRO)
// =========================
public async Task<string> ExportarExcel(
    string fechaDesde,
    string fechaHasta,
    string lote,
    string planta,
    string tipo
)
{
    using var conn = _db.GetConsumoPapelConnection();
    conn.Open();

    var (where, parameters) = BuildWhere(fechaDesde, fechaHasta, lote, planta, tipo);

    var sql = $@"
        SELECT 
            p.id,
            p.fecha,
            p.planta,
            p.tipo_movimiento,
            p.ean13,
            p.cantidad,
            p.lote
        FROM consumo_papel.palets_movimientos p
        {where}
        ORDER BY p.fecha DESC
    ";

    using var cmd = new MySqlCommand(sql, conn);

    foreach (var p in parameters)
        cmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));

    using var reader = cmd.ExecuteReader();

    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var ws = workbook.Worksheets.Add("Palets");

    // HEADERS
    ws.Cell(1, 1).Value = "ID";
    ws.Cell(1, 2).Value = "Fecha";
    ws.Cell(1, 3).Value = "Planta";
    ws.Cell(1, 4).Value = "Movimiento";
    ws.Cell(1, 5).Value = "EAN13";
    ws.Cell(1, 6).Value = "Detalle";
    ws.Cell(1, 7).Value = "Cantidad";
    ws.Cell(1, 8).Value = "Lote";

    int row = 2;

    while (reader.Read())
    {
        var ean = reader["ean13"]?.ToString() ?? "";

        ws.Cell(row, 1).Value = reader["id"]?.ToString();
        ws.Cell(row, 2).Value = reader["fecha"]?.ToString();
        ws.Cell(row, 3).Value = reader["planta"]?.ToString();
        ws.Cell(row, 4).Value = reader["tipo_movimiento"]?.ToString();
        ws.Cell(row, 5).Value = ean;
        ws.Cell(row, 6).Value = ObtenerDetalleSap(ean);
        ws.Cell(row, 7).Value = reader["cantidad"]?.ToString();
        ws.Cell(row, 8).Value = reader["lote"]?.ToString();

        row++;
    }

    ws.Columns().AdjustToContents();

    var fileName = $"palets_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

    var path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        fileName
    );

    workbook.SaveAs(path);

    // 🔥 ABRIR AUTOMÁTICO
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = path,
        UseShellExecute = true
    });

    return path;
}


    }

}