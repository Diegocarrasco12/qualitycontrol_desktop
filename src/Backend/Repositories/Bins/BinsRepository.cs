using System;
using System.Collections.Generic;
using MySqlConnector;
using LogisticControlCenter.Modules.Bins;
using LogisticControlCenter.Services;
using ClosedXML.Excel;

namespace LogisticControlCenter.Repositories.Bins
{
    public class BinsRepository
    {
        private readonly DbService _db;

        public BinsRepository(DbService db)
        {
            _db = db;
        }

        // =========================
        // LISTADO PAGINADO (ESTILO CONSUMO)
        // =========================
        public async Task<BinsPagedResult> ObtenerRegistros(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string bin,
            string calle,
            string tipo,
            string documento
        )
        {
            var result = new BinsPagedResult();

            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            var where = new List<string>();
            var parameters = new List<MySqlParameter>();

            // 🔥 BASE (igual que PHP)
            where.Add("m.tipo IN ('ENTRADA','SALIDA')");

            // =========================
            // FILTROS
            // =========================

            if (!string.IsNullOrEmpty(fechaDesde))
            {
                where.Add("m.fecha >= @fechaDesde");
                parameters.Add(new MySqlParameter("@fechaDesde", $"{fechaDesde} 00:00:00"));
            }

            if (!string.IsNullOrEmpty(fechaHasta))
            {
                where.Add("m.fecha <= @fechaHasta");
                parameters.Add(new MySqlParameter("@fechaHasta", $"{fechaHasta} 23:59:59"));
            }

            if (!string.IsNullOrEmpty(bin))
            {
                where.Add("(b.bin_codigo LIKE @bin OR CAST(b.numero_bin AS CHAR) LIKE @bin OR CAST(b.id AS CHAR) = @binExact)");
                parameters.Add(new MySqlParameter("@bin", $"%{bin}%"));
                parameters.Add(new MySqlParameter("@binExact", bin));
            }

            if (!string.IsNullOrEmpty(calle))
            {
                where.Add("b.calle LIKE @calle");
                parameters.Add(new MySqlParameter("@calle", $"%{calle}%"));
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                where.Add("m.tipo = @tipo");
                parameters.Add(new MySqlParameter("@tipo", tipo));
            }

            if (!string.IsNullOrEmpty(documento))
            {
                where.Add("m.documento LIKE @documento");
                parameters.Add(new MySqlParameter("@documento", $"%{documento}%"));
            }

            string whereSql = where.Count > 0
                ? "WHERE " + string.Join(" AND ", where)
                : "";

            int offset = (page - 1) * limit;

            // =========================
            // DATA
            // =========================
            string sql = $@"
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
                FROM control_bins.movimientos_bins m
                INNER JOIN control_bins.bins b ON b.id = m.bin_id
                {whereSql}
                ORDER BY m.fecha DESC
                LIMIT @limit OFFSET @offset;
            ";

            using var cmd = new MySqlCommand(sql, conn);

            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<BinsItem>();

            while (await reader.ReadAsync())
            {
                items.Add(new BinsItem
                {
                    Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                    Fecha = reader["fecha"]?.ToString() ?? "",
                    NumeroBin = reader["numero_bin"] != DBNull.Value ? Convert.ToInt32(reader["numero_bin"]) : 0,
                    Documento = reader["documento"]?.ToString() ?? "",
                    Tipo = reader["tipo"]?.ToString() ?? "",
                    Proveedor = reader["proveedor"]?.ToString() ?? "",
                    EstadoBin = reader["estado_bin"]?.ToString() ?? "",
                    BinCodigo = reader["bin_codigo"]?.ToString() ?? "",
                    Calle = reader["calle"]?.ToString() ?? "",
                    Archivo = reader["archivo"]?.ToString() ?? ""
                });
            }

            await reader.CloseAsync();

            // =========================
            // TOTAL
            // =========================
            string countSql = $@"
                SELECT COUNT(*)
                FROM control_bins.movimientos_bins m
                INNER JOIN control_bins.bins b ON b.id = m.bin_id
                {whereSql};
            ";

            using var countCmd = new MySqlCommand(countSql, conn);

            foreach (var p in parameters)
                countCmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));

            var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            result.Items = items;
            result.Total = total;
            result.Pages = (int)Math.Ceiling((double)total / limit);

            return result;
        }

        // =========================
// KPI HOY
// =========================
public async Task<(int entradas, int salidas)> ObtenerKPIHoy()
{
    using var conn = _db.GetConsumoPapelConnection();
    await conn.OpenAsync();

    string sql = @"
        SELECT 
            SUM(CASE WHEN tipo = 'ENTRADA' THEN 1 ELSE 0 END) AS entradas,
            SUM(CASE WHEN tipo = 'SALIDA' THEN 1 ELSE 0 END) AS salidas
        FROM control_bins.movimientos_bins
        WHERE fecha >= CURDATE()
          AND fecha < CURDATE() + INTERVAL 1 DAY;
    ";

    using var cmd = new MySqlCommand(sql, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    int entradas = 0;
    int salidas = 0;

    if (await reader.ReadAsync())
    {
        entradas = reader["entradas"] != DBNull.Value ? Convert.ToInt32(reader["entradas"]) : 0;
        salidas = reader["salidas"] != DBNull.Value ? Convert.ToInt32(reader["salidas"]) : 0;
    }

    return (entradas, salidas);
}

        // =========================
        // UPDATE (igual patrón consumo)
        // =========================
        public async Task GuardarCambios(List<BinsCambioItem> cambios)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var c in cambios)
                {
                    string sql = @"
                        UPDATE control_bins.movimientos_bins
                        SET documento = @documento,
                            estado_bin = @estado_bin
                        WHERE id = @id;
                    ";

                    using var cmd = new MySqlCommand(sql, conn, (MySqlTransaction)tx);

                    cmd.Parameters.Add("@documento", MySqlDbType.VarChar).Value = c.Documento ?? "";
                    cmd.Parameters.Add("@estado_bin", MySqlDbType.VarChar).Value = c.EstadoBin ?? "";
                    cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = c.Id;

                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR UPDATE BINS: {ex}");

                await tx.RollbackAsync();
                throw;
            }
        }

        // =========================
// EXPORTAR EXCEL (ESTILO CONSUMO)
// =========================
public async Task<string> ExportarExcel(
    string fechaDesde,
    string fechaHasta,
    string bin,
    string calle,
    string tipo,
    string documento
)
{
    using var conn = _db.GetConsumoPapelConnection();
    await conn.OpenAsync();

    var where = new List<string>();
    var parameters = new List<MySqlParameter>();

    // 🔥 BASE
    where.Add("m.tipo IN ('ENTRADA','SALIDA')");

    // =========================
    // FILTROS (IGUAL QUE LISTADO)
    // =========================

    if (!string.IsNullOrEmpty(fechaDesde))
    {
        where.Add("m.fecha >= @fechaDesde");
        parameters.Add(new MySqlParameter("@fechaDesde", $"{fechaDesde} 00:00:00"));
    }

    if (!string.IsNullOrEmpty(fechaHasta))
    {
        where.Add("m.fecha <= @fechaHasta");
        parameters.Add(new MySqlParameter("@fechaHasta", $"{fechaHasta} 23:59:59"));
    }

    if (!string.IsNullOrEmpty(bin))
    {
        where.Add("(b.bin_codigo LIKE @bin OR CAST(b.numero_bin AS CHAR) LIKE @bin OR CAST(b.id AS CHAR) = @binExact)");
        parameters.Add(new MySqlParameter("@bin", $"%{bin}%"));
        parameters.Add(new MySqlParameter("@binExact", bin));
    }

    if (!string.IsNullOrEmpty(calle))
    {
        where.Add("b.calle LIKE @calle");
        parameters.Add(new MySqlParameter("@calle", $"%{calle}%"));
    }

    if (!string.IsNullOrEmpty(tipo))
    {
        where.Add("m.tipo = @tipo");
        parameters.Add(new MySqlParameter("@tipo", tipo));
    }

    if (!string.IsNullOrEmpty(documento))
    {
        where.Add("m.documento LIKE @documento");
        parameters.Add(new MySqlParameter("@documento", $"%{documento}%"));
    }

    string whereSql = where.Count > 0
        ? "WHERE " + string.Join(" AND ", where)
        : "";

    // =========================
    // QUERY COMPLETA (SIN LIMIT)
    // =========================
    string sql = $@"
        SELECT
            m.id,
            m.fecha,
            b.numero_bin,
            m.documento,
            m.proveedor,
            m.estado_bin,
            b.bin_codigo,
            b.calle,
            m.tipo
        FROM control_bins.movimientos_bins m
        INNER JOIN control_bins.bins b ON b.id = m.bin_id
        {whereSql}
        ORDER BY m.fecha DESC;
    ";

    using var cmd = new MySqlCommand(sql, conn);

    foreach (var p in parameters)
        cmd.Parameters.Add(p);

    using var reader = await cmd.ExecuteReaderAsync();

    // =========================
    // 🔥 EXCEL (ClosedXML)
    // =========================
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var ws = workbook.Worksheets.Add("BINS");

    // HEADER
    ws.Cell(1, 1).Value = "ID";
    ws.Cell(1, 2).Value = "Fecha";
    ws.Cell(1, 3).Value = "Numero BIN";
    ws.Cell(1, 4).Value = "Documento";
    ws.Cell(1, 5).Value = "Proveedor";
    ws.Cell(1, 6).Value = "Estado BIN";
    ws.Cell(1, 7).Value = "BIN Codigo";
    ws.Cell(1, 8).Value = "Calle";
    ws.Cell(1, 9).Value = "Tipo";

    int row = 2;

    while (await reader.ReadAsync())
    {
        ws.Cell(row, 1).Value = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;

ws.Cell(row, 2).Value = reader["fecha"]?.ToString() ?? "";

ws.Cell(row, 3).Value = reader["numero_bin"] != DBNull.Value ? Convert.ToInt32(reader["numero_bin"]) : 0;

ws.Cell(row, 4).Value = reader["documento"]?.ToString() ?? "";

ws.Cell(row, 5).Value = reader["proveedor"]?.ToString() ?? "";

ws.Cell(row, 6).Value = reader["estado_bin"]?.ToString() ?? "";

ws.Cell(row, 7).Value = reader["bin_codigo"]?.ToString() ?? "";

ws.Cell(row, 8).Value = reader["calle"]?.ToString() ?? "";

ws.Cell(row, 9).Value = reader["tipo"]?.ToString() ?? "";

        row++;
    }

    // =========================
    // GUARDAR
    // =========================
    var fileName = $"BINS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

    var path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        fileName
    );

    workbook.SaveAs(path);

    return path;
}


    }
}