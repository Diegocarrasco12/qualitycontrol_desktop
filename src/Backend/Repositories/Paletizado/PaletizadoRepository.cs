using LogisticControlCenter.Services;
using MySqlConnector;

namespace LogisticControlCenter.Repositories.Paletizado
{
    public class PaletizadoRepository
    {
        private readonly DbService _db;

        public PaletizadoRepository(DbService db)
        {
            _db = db;
        }

        // =========================
        // LISTADO PAGINADO
        // =========================
        public async Task<(List<dynamic> Items, int Total, int Pages)> ObtenerPalets(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string planta,
            string taller,
            string np,
            string idPalet
        )
        {
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

            int offset = (page - 1) * limit;

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
                    p.unidades_por_pliego,
                    p.valor_unitario,
                    p.valor_total_palet,
                    COALESCE(p.descripcion_sap, p.descripcion) AS descripcion,
                    p.fecha_impresion,
                    u.nombre_completo AS emisor_tarja
                FROM palets p
                LEFT JOIN etiquetas_temp et ON et.correlativo = p.id_palet
                LEFT JOIN usuarios u ON u.id = et.created_by_user_id
                {whereSql}
                ORDER BY p.fecha_registro DESC
                LIMIT @limit OFFSET @offset;
            ";

            using var cmd = new MySqlCommand(sql, conn);

            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<dynamic>();

            while (await reader.ReadAsync())
            {
                items.Add(
                    new
                    {
                        IdPalet = reader["id_palet"]?.ToString(),
                        Fecha = reader["fecha_registro"] != DBNull.Value
                            ? Convert.ToDateTime(reader["fecha_registro"])
                            : (DateTime?)null,
                        Planta = reader["planta_produccion"]?.ToString(),
                        NPCliente = reader["np_cliente"]?.ToString(),
                        NPInnpack = reader["nota_venta_innpack"]?.ToString(),
                        NombreCliente = reader["nombre_cliente"]?.ToString(),
                        Taller = reader["taller_paletizado"]?.ToString(),
                        Tipo = reader["tipo_palet"]?.ToString(),
                        Cantidad = reader["cantidad"] != DBNull.Value
                            ? Convert.ToDecimal(reader["cantidad"])
                            : 0,
                        UnidadesPorPliego = reader["unidades_por_pliego"]?.ToString(),
                        ValorUnitario = reader["valor_unitario"] != DBNull.Value
                            ? Convert.ToDecimal(reader["valor_unitario"])
                            : 0,
                        ValorTotal = reader["valor_total_palet"] != DBNull.Value
                            ? Convert.ToDecimal(reader["valor_total_palet"])
                            : 0,
                        Descripcion = reader["descripcion"]?.ToString(),
                        FechaImpresion = reader["fecha_impresion"]?.ToString(),
                        EmisorTarja = reader["emisor_tarja"]?.ToString(),
                    }
                );
            }

            await reader.CloseAsync();

            // =========================
            // TOTAL
            // =========================

            string countSql = $@"SELECT COUNT(*) FROM palets p {whereSql};";

            using var countCmd = new MySqlCommand(countSql, conn);

            foreach (var p in parameters)
                countCmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));

            var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            int pages = (int)Math.Ceiling((double)total / limit);

            return (items, total, pages);
        }
    }
}
