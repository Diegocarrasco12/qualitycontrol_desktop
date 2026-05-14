using System;
using System.Collections.Generic;
using MySqlConnector;
using LogisticControlCenter.Services;
using LogisticControlCenter.Modules.BinsLavado;
using ClosedXML.Excel;
using System.IO;

namespace LogisticControlCenter.Repositories.BinsLavado
{
    public class BinsLavadoRepository
    {
        private readonly DbService _db;

        public BinsLavadoRepository(DbService db)
        {
            _db = db;
        }

        // =========================
        // OBTENER LAVADOS
        // =========================
        public (List<BinsLavadoItem> items, int total, int pages) ObtenerLavados(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string bin,
            string calle,
            string documento
        )
        {
            var lista = new List<BinsLavadoItem>();

            using var conn = _db.GetBinsConnection();
            conn.Open();

            var (where, parameters) = BuildWhere(fechaDesde, fechaHasta, bin, calle, documento);

            // =========================
            // TOTAL
            // =========================
            int total = 0;

            var countSql = $@"
                SELECT COUNT(*) 
                FROM movimientos_bins m
                INNER JOIN bins b ON b.id = m.bin_id
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
                    m.id,
                    m.fecha,
                    b.numero_bin,
                    m.documento,
                    m.proveedor,
                    m.estado_bin,
                    b.bin_codigo,
                    b.calle,
                    m.tipo,
                    m.archivo
                FROM movimientos_bins m
                INNER JOIN bins b ON b.id = m.bin_id
                {where}
                ORDER BY m.fecha DESC
                LIMIT @limit OFFSET @offset
            ";

            using var cmd = new MySqlCommand(sql, conn);

            foreach (var p in parameters)
                cmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));

            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new BinsLavadoItem
                {
                    Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                    Fecha = reader["fecha"] != DBNull.Value
                        ? Convert.ToDateTime(reader["fecha"]).ToString("yyyy-MM-dd HH:mm:ss")
                        : "",
                    NumeroBin = reader["numero_bin"] != DBNull.Value ? Convert.ToInt32(reader["numero_bin"]) : 0,
                    Documento = reader["documento"]?.ToString() ?? "",
                    Proveedor = reader["proveedor"]?.ToString() ?? "",
                    EstadoBin = reader["estado_bin"]?.ToString() ?? "",
                    BinCodigo = reader["bin_codigo"]?.ToString() ?? "",
                    Calle = reader["calle"]?.ToString() ?? "",
                    Tipo = reader["tipo"]?.ToString() ?? "",
                    Archivo = reader["archivo"]?.ToString() ?? ""
                });
            }

            return (lista, total, pages);
        }

        // =========================
        // KPI HOY (🔥 NUEVO)
        // =========================
        public int ObtenerKPIHoy()
        {
            using var conn = _db.GetBinsConnection();
            conn.Open();

            var sql = @"
                SELECT COUNT(*) 
                FROM movimientos_bins
                WHERE tipo = 'LAVADO'
                AND DATE(fecha) = CURDATE()
            ";

            using var cmd = new MySqlCommand(sql, conn);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // =========================
        // BUILDER WHERE (🔥 PRO)
        // =========================
        private (string where, List<MySqlParameter> parameters) BuildWhere(
            string fechaDesde,
            string fechaHasta,
            string bin,
            string calle,
            string documento
        )
        {
            var where = "WHERE m.tipo = 'LAVADO'";
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(fechaDesde))
            {
                where += " AND m.fecha >= @desde";
                parameters.Add(new MySqlParameter("@desde", fechaDesde + " 00:00:00"));
            }

            if (!string.IsNullOrEmpty(fechaHasta))
            {
                where += " AND m.fecha <= @hasta";
                parameters.Add(new MySqlParameter("@hasta", fechaHasta + " 23:59:59"));
            }

            if (!string.IsNullOrEmpty(bin))
            {
                where += " AND (b.bin_codigo LIKE @bin OR CAST(b.numero_bin AS CHAR) LIKE @bin OR CAST(b.id AS CHAR) = @binExact)";
                parameters.Add(new MySqlParameter("@bin", "%" + bin + "%"));
                parameters.Add(new MySqlParameter("@binExact", bin));
            }

            if (!string.IsNullOrEmpty(calle))
            {
                where += " AND b.calle LIKE @calle";
                parameters.Add(new MySqlParameter("@calle", "%" + calle + "%"));
            }

            if (!string.IsNullOrEmpty(documento))
            {
                where += " AND m.documento LIKE @doc";
                parameters.Add(new MySqlParameter("@doc", "%" + documento + "%"));
            }

            return (where, parameters);
        }
        // =========================
// EXPORTAR EXCEL (PRO)
// =========================
public async Task<string> ExportarExcel(
    string fechaDesde,
    string fechaHasta,
    string bin,
    string calle,
    string documento
)
{
    using var conn = _db.GetBinsConnection();
    conn.Open();

    var (where, parameters) = BuildWhere(fechaDesde, fechaHasta, bin, calle, documento);

    var sql = $@"
        SELECT
            m.id,
            m.fecha,
            b.numero_bin,
            m.documento,
            m.proveedor,
            m.estado_bin,
            b.bin_codigo,
            b.calle,
            m.tipo,
            m.archivo
        FROM movimientos_bins m
        INNER JOIN bins b ON b.id = m.bin_id
        {where}
        ORDER BY m.fecha DESC
    ";

    using var cmd = new MySqlCommand(sql, conn);

    foreach (var p in parameters)
        cmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));

    using var reader = cmd.ExecuteReader();

    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var ws = workbook.Worksheets.Add("LavadoBins");

    ws.Cell(1, 1).Value = "ID";
    ws.Cell(1, 2).Value = "Fecha";
    ws.Cell(1, 3).Value = "Numero BIN";
    ws.Cell(1, 4).Value = "Documento";
    ws.Cell(1, 5).Value = "Proveedor";
    ws.Cell(1, 6).Value = "Estado";
    ws.Cell(1, 7).Value = "BIN Codigo";
    ws.Cell(1, 8).Value = "Calle";
    ws.Cell(1, 9).Value = "Tipo";
    ws.Cell(1, 10).Value = "Archivo";

    int row = 2;

    while (reader.Read())
    {
        ws.Cell(row, 1).Value = reader["id"]?.ToString();
        ws.Cell(row, 2).Value = reader["fecha"]?.ToString();
        ws.Cell(row, 3).Value = reader["numero_bin"]?.ToString();
        ws.Cell(row, 4).Value = reader["documento"]?.ToString();
        ws.Cell(row, 5).Value = reader["proveedor"]?.ToString();
        ws.Cell(row, 6).Value = reader["estado_bin"]?.ToString();
        ws.Cell(row, 7).Value = reader["bin_codigo"]?.ToString();
        ws.Cell(row, 8).Value = reader["calle"]?.ToString();
        ws.Cell(row, 9).Value = reader["tipo"]?.ToString();
        ws.Cell(row, 10).Value = reader["archivo"]?.ToString();

        row++;
    }

    ws.Columns().AdjustToContents();

    var fileName = $"lavado_bins_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

    var path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        fileName
    );

    workbook.SaveAs(path);

// 🔥 ABRIR AUTOMÁTICAMENTE
System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
{
    FileName = path,
    UseShellExecute = true
});

return path;
}
    }
    


}