using LogisticControlCenter.Modules.ConsumoPapel;
using LogisticControlCenter.Services;
using MySqlConnector;

namespace LogisticControlCenter.Repositories.ConsumoPapel
{
    public class ConsumoPapelRepository
    {
        private readonly DbService _db;

        public ConsumoPapelRepository(DbService db)
        {
            _db = db;
        }

        // =========================
        // LISTADO PAGINADO
        // =========================
        public async Task<ConsumoPapelPagedResult> ObtenerConsumos(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string codigo,
            string lote
        )
        {
            var result = new ConsumoPapelPagedResult();

            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            var where = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(fechaDesde))
            {
                where.Add("fecha >= @fechaDesde");
                parameters.Add(new MySqlParameter("@fechaDesde", fechaDesde));
            }

            if (!string.IsNullOrEmpty(fechaHasta))
            {
                where.Add("fecha < @hasta");
                parameters.Add(new MySqlParameter("@hasta", DateTime.Parse(fechaHasta).AddDays(1)));
            }

            if (!string.IsNullOrEmpty(codigo))
            {
                where.Add("codigo LIKE @codigo");
                parameters.Add(new MySqlParameter("@codigo", $"%{codigo}%"));
            }

            if (!string.IsNullOrEmpty(lote))
            {
                where.Add("lote LIKE @lote");
                parameters.Add(new MySqlParameter("@lote", $"%{lote}%"));
            }

            string whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            int offset = (page - 1) * limit;

            string sql =
                $@"
                SELECT 
                    id,
                    fecha,
                    descripcion,
                    codigo,
                    consumo_kg,
                    np,
                    tarja_kg,
                    saldo_kg,
lote,
ubicacion_bin,
estado,
                    salida
                FROM tarjas_scan
                {whereSql}
                ORDER BY id DESC
                LIMIT @limit OFFSET @offset;
            ";

            using var cmd = new MySqlCommand(sql, conn);

            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<ConsumoPapelItem>();

            while (await reader.ReadAsync())
            {
                items.Add(
                    new ConsumoPapelItem
                    {
                        Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                        Fecha =
                            reader["fecha"] != DBNull.Value
                                ? Convert.ToDateTime(reader["fecha"])
                                : DateTime.MinValue,
                        Descripcion = reader["descripcion"]?.ToString() ?? "",
                        Codigo = reader["codigo"]?.ToString() ?? "",
                        ConsumoKg =
                            reader["consumo_kg"] != DBNull.Value
                                ? Convert.ToDecimal(reader["consumo_kg"])
                                : 0,
                        NP = reader["np"]?.ToString() ?? "",
                        TarjaKg =
                            reader["tarja_kg"] != DBNull.Value
                                ? Convert.ToDecimal(reader["tarja_kg"])
                                : 0,
                        SaldoKg =
                            reader["saldo_kg"] != DBNull.Value
                                ? Convert.ToDecimal(reader["saldo_kg"])
                                : 0,
                        Lote = reader["lote"]?.ToString() ?? "",
                        UbicacionBin = reader["ubicacion_bin"]?.ToString() ?? "",
                        Estado = reader["estado"]?.ToString() ?? "",
                        Salida = reader["salida"]?.ToString() ?? "",
                    }
                );
            }

            await reader.CloseAsync();

            // TOTAL
            string countSql = $@"SELECT COUNT(*) FROM tarjas_scan {whereSql};";

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
        // KPIs
        // =========================
        public async Task<ConsumoKpiItem> ObtenerKpis()
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            var kpi = new ConsumoKpiItem();

            string sql =
                @"
                SELECT 
                    SUM(CASE 
                        WHEN fecha >= CURDATE() 
                         AND fecha < CURDATE() + INTERVAL 1 DAY 
                        THEN consumo_kg 
                        ELSE 0 
                    END) as consumoHoy,
                    COUNT(*) as tarjasHoy
                FROM tarjas_scan;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                kpi.ConsumoHoy =
                    reader["consumoHoy"] != DBNull.Value
                        ? Convert.ToDecimal(reader["consumoHoy"])
                        : 0;
                kpi.TarjasHoy =
                    reader["tarjasHoy"] != DBNull.Value ? Convert.ToInt32(reader["tarjasHoy"]) : 0;
            }

            await reader.CloseAsync();

            using var saldoCmd = new MySqlCommand("SELECT SUM(saldo_kg) FROM tarjas_scan;", conn);
            var saldo = await saldoCmd.ExecuteScalarAsync();
            kpi.SaldoTotal = saldo != DBNull.Value && saldo != null ? Convert.ToDecimal(saldo) : 0;

            string lastSql = "SELECT codigo, lote FROM tarjas_scan ORDER BY id DESC LIMIT 1;";
            using var lastCmd = new MySqlCommand(lastSql, conn);
            using var lastReader = await lastCmd.ExecuteReaderAsync();

            if (await lastReader.ReadAsync())
            {
                kpi.UltimoCodigo = lastReader["codigo"]?.ToString() ?? "";
                kpi.UltimoLote = lastReader["lote"]?.ToString() ?? "";
            }

            return kpi;
        }

        // =========================
        // UPDATE (FIX DEFINITIVO)
        // =========================
        public async Task GuardarCambios(List<ConsumoCambioItem> cambios)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var c in cambios)
                {
                    string sql =
                        @"
                        UPDATE tarjas_scan
                        SET estado = @estado,
                            salida = @salida
                        WHERE id = @id;
                    ";

                    using var cmd = new MySqlCommand(sql, conn, (MySqlTransaction)tx);

                    cmd.Parameters.Add("@estado", MySqlDbType.VarChar).Value = c.Estado ?? "";
                    cmd.Parameters.Add("@salida", MySqlDbType.VarChar).Value = c.Salida ?? "";
                    cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = c.Id;

                    await cmd.ExecuteNonQueryAsync();
                    // 🔥 YA NO VALIDAMOS rows (esto era el bug)
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR UPDATE REAL: {ex}");

                await tx.RollbackAsync();

                throw;
            }
        }
    }
}
