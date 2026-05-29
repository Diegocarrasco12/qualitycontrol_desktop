using MySqlConnector;
using QualityControlCenter.Services;

namespace QualityControlCenter.Modules.Home
{
    public class HomeService
    {
        private readonly DbService _db;

        public HomeService(DbService db)
        {
            _db = db;
        }

        public async Task<(int bins, int lavado, int palets, int consumo, int altillo)> GetDashboard()
        {
            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            var totalRegistros = await Count(conn, "SELECT COUNT(*) FROM registros_control;");
            var calidad = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE UPPER(IFNULL(area, '')) = 'CALIDAD';");
            var produccion = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE UPPER(IFNULL(area, '')) = 'PRODUCCION';");
            var maquinas = await Count(conn, "SELECT COUNT(*) FROM maquinas WHERE activo = 1;");
            var usuarios = await Count(conn, "SELECT COUNT(*) FROM usuarios WHERE activo = 1;");

            return (totalRegistros, calidad, produccion, maquinas, usuarios);
        }

        public async Task<(int bins, int lavado, int palets, int consumo, int altillo)> GetUltimos7Dias()
        {
            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            var totalRegistros = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE fecha_registro >= CURDATE() - INTERVAL 6 DAY;");
            var calidad = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE UPPER(IFNULL(area, '')) = 'CALIDAD' AND fecha_registro >= CURDATE() - INTERVAL 6 DAY;");
            var produccion = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE UPPER(IFNULL(area, '')) = 'PRODUCCION' AND fecha_registro >= CURDATE() - INTERVAL 6 DAY;");
            var maquinas = await Count(conn, "SELECT COUNT(DISTINCT maquina_id) FROM registros_control WHERE fecha_registro >= CURDATE() - INTERVAL 6 DAY;");
            var usuarios = await Count(conn, "SELECT COUNT(DISTINCT usuario_id) FROM registros_control WHERE fecha_registro >= CURDATE() - INTERVAL 6 DAY;");

            return (totalRegistros, calidad, produccion, maquinas, usuarios);
        }

        public async Task<(int bins, int lavado, int palets, int consumo, int altillo)> GetHoy()
        {
            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            var totalRegistros = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE fecha_registro = CURDATE();");
            var calidad = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE UPPER(IFNULL(area, '')) = 'CALIDAD' AND fecha_registro = CURDATE();");
            var produccion = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE UPPER(IFNULL(area, '')) = 'PRODUCCION' AND fecha_registro = CURDATE();");
            var maquinas = await Count(conn, "SELECT COUNT(DISTINCT maquina_id) FROM registros_control WHERE fecha_registro = CURDATE();");
            var usuarios = await Count(conn, "SELECT COUNT(DISTINCT usuario_id) FROM registros_control WHERE fecha_registro = CURDATE();");

            return (totalRegistros, calidad, produccion, maquinas, usuarios);
        }

        public async Task<List<object>> ObtenerRegistrosUltimos7Dias()
{
    var lista = new List<object>();

    using var conn = _db.GetCalidadConnection();
    await conn.OpenAsync();

    using var cmd = new MySqlCommand(@"
        SELECT
            DATE(rc.fecha_registro) AS fecha,

            SUM(CASE
                WHEN LOWER(IFNULL(ec.nombre, '')) LIKE '%aprob%' THEN 1
                ELSE 0
            END) AS aprobados,

            SUM(CASE
                WHEN LOWER(IFNULL(ec.nombre, '')) LIKE '%rech%' THEN 1
                ELSE 0
            END) AS rechazados,

            SUM(CASE
                WHEN LOWER(IFNULL(ec.nombre, '')) NOT LIKE '%aprob%'
                 AND LOWER(IFNULL(ec.nombre, '')) NOT LIKE '%rech%'
                THEN 1
                ELSE 0
            END) AS observados

        FROM registros_control rc
        LEFT JOIN estados_catalogo ec ON rc.estado_id = ec.id

        WHERE rc.fecha_registro >= CURDATE() - INTERVAL 6 DAY
          AND UPPER(IFNULL(rc.area, '')) = 'CALIDAD'

        GROUP BY DATE(rc.fecha_registro)
        ORDER BY fecha ASC;
    ", conn);

    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        lista.Add(new
        {
            fecha = Convert.ToDateTime(reader["fecha"]).ToString("dd-MM"),
            aprobados = Convert.ToInt32(reader["aprobados"]),
            rechazados = Convert.ToInt32(reader["rechazados"]),
            observados = Convert.ToInt32(reader["observados"])
        });
    }

    return lista;
}

        public async Task<object> ObtenerEstadosHoy()
        {
            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            int aprobados = 0;
            int rechazados = 0;
            int observados = 0;

            using var cmd = new MySqlCommand(@"
                SELECT
                    IFNULL(ec.nombre, 'SIN ESTADO') AS estado,
                    COUNT(*) AS total
                FROM registros_control rc
                LEFT JOIN estados_catalogo ec ON rc.estado_id = ec.id
                WHERE rc.fecha_registro = CURDATE()
                GROUP BY ec.nombre;
            ", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var estado = reader["estado"]?.ToString()?.ToLower() ?? "";
                var total = Convert.ToInt32(reader["total"]);

                if (estado.Contains("aprob"))
                    aprobados += total;
                else if (estado.Contains("rech"))
                    rechazados += total;
                else
                    observados += total;
            }

            return new
            {
                aprobados,
                rechazados,
                observados
            };
        }

        public async Task<List<object>> ObtenerActividadReciente()
        {
            var lista = new List<object>();

            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT
                    CONCAT('Registro ', IFNULL(area, 'SIN ÁREA'), ' - NP ', IFNULL(np, '-')) AS descripcion,
                    creado_en AS fecha
                FROM registros_control
                ORDER BY id DESC
                LIMIT 5;
            ", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new
                {
                    descripcion = reader["descripcion"]?.ToString() ?? "",
                    fecha = reader["fecha"]?.ToString() ?? ""
                });
            }

            return lista;
        }

        public async Task<List<string>> ObtenerAlertas()
        {
            var alertas = new List<string>();

            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            var sinArea = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE area IS NULL OR area = '';");
            if (sinArea > 0)
                alertas.Add($"⚠ {sinArea} registros antiguos sin área");

            var observacionesHoy = await Count(conn, "SELECT COUNT(*) FROM registros_control WHERE fecha_registro = CURDATE() AND observacion IS NOT NULL AND TRIM(observacion) <> '';");
            if (observacionesHoy > 0)
                alertas.Add($"⚠ {observacionesHoy} registros con observación hoy");

            return alertas;
        }

        private async Task<int> Count(MySqlConnection conn, string sql)
        {
            using var cmd = new MySqlCommand(sql, conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
        }
    }
}
