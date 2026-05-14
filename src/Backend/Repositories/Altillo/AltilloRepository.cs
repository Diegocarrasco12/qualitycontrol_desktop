using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MySqlConnector;
using LogisticControlCenter.Modules.Altillo;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Repositories.Altillo
{
    public class AltilloRepository
    {
        private readonly DbService _db;

        public AltilloRepository(DbService db)
        {
            _db = db;
        }

        // =========================
        // OBTENER REGISTROS
        // =========================
        public (List<AltilloItem> data, int total) ObtenerRegistros(
            string? fechaDesde,
            string? fechaHasta,
            string? codigo,
            string? lote,
            int page,
            int limit
        )
        {
            var lista = new List<AltilloItem>();

            using var conn = _db.GetConsumoPapelConnection();
            conn.Open();

            int offset = (page - 1) * limit;

            var where = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(fechaDesde))
            {
                where.Add("fecha >= @fechaDesde");
                parameters.Add(new MySqlParameter("@fechaDesde", fechaDesde));
            }

            if (!string.IsNullOrWhiteSpace(fechaHasta))
            {
                where.Add("fecha <= @fechaHasta");
                parameters.Add(new MySqlParameter("@fechaHasta", fechaHasta + " 23:59:59"));
            }

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                where.Add("codigo LIKE @codigo");
                parameters.Add(new MySqlParameter("@codigo", $"%{codigo}%"));
            }

            if (!string.IsNullOrWhiteSpace(lote))
            {
                where.Add("lote LIKE @lote");
                parameters.Add(new MySqlParameter("@lote", $"%{lote}%"));
            }

            string whereSql = where.Count > 0
                ? "WHERE " + string.Join(" AND ", where)
                : "";

            int total = 0;

            string countSql = $@"
                SELECT COUNT(*)
                FROM altillo_scan
                {whereSql}
            ";

            using (var countCmd = new MySqlCommand(countSql, conn))
            {
                countCmd.Parameters.AddRange(parameters.ToArray());
                total = Convert.ToInt32(countCmd.ExecuteScalar());
            }

            string sql = $@"
                SELECT
                    id,
                    fecha,
                    nombre,
                    descripcion,
                    codigo,
                    consumo,
                    np,
                    unidades_tarja,
                    saldo,
                    lote,
                    comentario,
                    estado,
                    extra_post_estado,
                    created_at
                FROM altillo_scan
                {whereSql}
                ORDER BY created_at DESC
                LIMIT @limit OFFSET @offset
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new AltilloItem
                {
                    Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                    Fecha = reader["fecha"] != DBNull.Value ? Convert.ToDateTime(reader["fecha"]) : DateTime.MinValue,
                    Nombre = reader["nombre"]?.ToString() ?? "",
                    Descripcion = reader["descripcion"]?.ToString() ?? "",
                    Codigo = reader["codigo"]?.ToString() ?? "",
                    Consumo = reader["consumo"] != DBNull.Value ? Convert.ToDecimal(reader["consumo"]) : 0,
                    NP = reader["np"]?.ToString() ?? "",
                    UnidadesTarja = reader["unidades_tarja"] != DBNull.Value ? Convert.ToDecimal(reader["unidades_tarja"]) : 0,
                    Saldo = reader["saldo"] != DBNull.Value ? Convert.ToDecimal(reader["saldo"]) : 0,
                    Lote = reader["lote"]?.ToString() ?? "",
                    Comentario = reader["comentario"]?.ToString() ?? "",
                    Estado = reader["estado"]?.ToString() ?? "",
                    ExtraPostEstado = reader["extra_post_estado"]?.ToString() ?? "",
                    CreatedAt = reader["created_at"] != DBNull.Value ? Convert.ToDateTime(reader["created_at"]) : DateTime.MinValue
                });
            }

            return (lista, total);
        }

        // =========================
        // 🔥 GUARDAR CAMBIOS (FIX REAL)
        // =========================
        public void GuardarCambios(List<AltilloCambioItem> cambios)
        {
            using var conn = _db.GetConsumoPapelConnection();
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                foreach (var item in cambios)
                {
                    if (item.Id <= 0)
                        throw new Exception($"ID inválido: {item.Id}");

                   string sql = @"
    UPDATE altillo_scan
    SET
        comentario = COALESCE(@comentario, comentario),
        estado = COALESCE(@estado, estado),
        extra_post_estado = COALESCE(@extra, extra_post_estado)
    WHERE id = @id
";

                    using var cmd = new MySqlCommand(sql, conn, transaction);
cmd.Parameters.AddWithValue("@comentario", item.Comentario);
cmd.Parameters.AddWithValue("@estado", item.Estado);
cmd.Parameters.AddWithValue("@extra", item.ExtraPostEstado);
cmd.Parameters.AddWithValue("@id", item.Id);

                    var rows = cmd.ExecuteNonQuery();

                    Console.WriteLine($"UPDATE ALTILLO → ID: {item.Id} | filas afectadas: {rows}");

                   
                    // 🔥 MySQL puede devolver 0 si no hay cambio real → NO es error
if (rows == 0)
{
    Console.WriteLine($"⚠️ SIN CAMBIO REAL → ID: {item.Id}");
}

} 

transaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR UPDATE ALTILLO: {ex.Message}");
                transaction.Rollback();
                throw;
            }
        }

        // =========================
        // EXPORTAR CSV
        // =========================
        public object ExportarExcel(
            string? fechaDesde,
            string? fechaHasta,
            string? codigo,
            string? lote
        )
        {
            using var conn = _db.GetConsumoPapelConnection();
            conn.Open();

            var where = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(fechaDesde))
            {
                where.Add("fecha >= @fechaDesde");
                parameters.Add(new MySqlParameter("@fechaDesde", fechaDesde));
            }

            if (!string.IsNullOrWhiteSpace(fechaHasta))
            {
                where.Add("fecha <= @fechaHasta");
                parameters.Add(new MySqlParameter("@fechaHasta", fechaHasta + " 23:59:59"));
            }

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                where.Add("codigo LIKE @codigo");
                parameters.Add(new MySqlParameter("@codigo", $"%{codigo}%"));
            }

            if (!string.IsNullOrWhiteSpace(lote))
            {
                where.Add("lote LIKE @lote");
                parameters.Add(new MySqlParameter("@lote", $"%{lote}%"));
            }

            string whereSql = where.Count > 0
                ? "WHERE " + string.Join(" AND ", where)
                : "";

            string sql = $@"
                SELECT
                    id,
                    fecha,
                    nombre,
                    descripcion,
                    codigo,
                    consumo,
                    np,
                    unidades_tarja,
                    saldo,
                    lote,
                    comentario,
                    estado,
                    extra_post_estado
                FROM altillo_scan
                {whereSql}
                ORDER BY created_at DESC
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            using var reader = cmd.ExecuteReader();

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"altillo_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = Path.Combine(desktop, fileName);

            using var writer = new StreamWriter(path, false, new UTF8Encoding(true));

            writer.WriteLine("ID;Fecha;Nombre;Descripción;Código;Consumo;NP;Tarja;Saldo;Lote;Comentario;Estado;Extra");

            while (reader.Read())
            {
                writer.WriteLine(string.Join(";",
                    EscapeCsv(reader["id"]),
                    EscapeCsv(reader["fecha"]),
                    EscapeCsv(reader["nombre"]),
                    EscapeCsv(reader["descripcion"]),
                    EscapeCsv(reader["codigo"]),
                    EscapeCsv(reader["consumo"]),
                    EscapeCsv(reader["np"]),
                    EscapeCsv(reader["unidades_tarja"]),
                    EscapeCsv(reader["saldo"]),
                    EscapeCsv(reader["lote"]),
                    EscapeCsv(reader["comentario"]),
                    EscapeCsv(reader["estado"]),
                    EscapeCsv(reader["extra_post_estado"])
                ));
            }

            writer.Flush();
            Console.WriteLine($"CSV ALTILLO generado: {path}");
System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
{
    FileName = path,
    UseShellExecute = true
});
            return new
{
    path = path
};
}
        private static string EscapeCsv(object? value)
        {
            var text = value?.ToString() ?? "";
            text = text.Replace("\"", "\"\"");
            return $"\"{text}\"";
        }
        // =========================
// KPIS
// =========================
public object ObtenerKpis()
{
    using var conn = _db.GetConsumoPapelConnection();
    conn.Open();

    // 🔥 TOTAL REGISTROS
    int totalRegistros = 0;
    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM altillo_scan", conn))
    {
        totalRegistros = Convert.ToInt32(cmd.ExecuteScalar());
    }

    // 🔥 SALDO TOTAL
    decimal saldoTotal = 0;
    using (var cmd = new MySqlCommand("SELECT COALESCE(SUM(saldo),0) FROM altillo_scan", conn))
    {
        saldoTotal = Convert.ToDecimal(cmd.ExecuteScalar());
    }

    // 🔥 ÚLTIMA FECHA
    DateTime? ultimoRegistro = null;
    using (var cmd = new MySqlCommand("SELECT MAX(fecha) FROM altillo_scan", conn))
    {
        var result = cmd.ExecuteScalar();
        if (result != DBNull.Value && result != null)
            ultimoRegistro = Convert.ToDateTime(result);
    }

    return new
    {
        totalRegistros,
        saldoTotal,
        ultimoRegistro
    };
}
    }
}