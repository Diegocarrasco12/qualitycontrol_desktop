using System;
using System.Threading.Tasks;
using MySqlConnector;
using QualityControlCenter.Services;

namespace QualityControlCenter.Modules.Dashboard
{
    public class DashboardRepository
    {
        private readonly DbService _db;

        public DashboardRepository(DbService db)
        {
            _db = db;
        }

        public async Task<DashboardResumenDto> ObtenerResumen()
        {
            var result = new DashboardResumenDto();

            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            const string filtroCalidad = "UPPER(IFNULL(area, '')) = 'CALIDAD'";

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT COUNT(*)
                FROM registros_control
                WHERE DATE(fecha_registro) = CURDATE()
                  AND {filtroCalidad};
            ",
                    conn
                )
            )
            {
                result.ControlesHoy = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT IFNULL(SUM(merma_insumos_desponche_bobinas), 0)
                FROM registros_control
                WHERE DATE(fecha_registro) = CURDATE()
                  AND {filtroCalidad};
            ",
                    conn
                )
            )
            {
                result.MermaInsumosHoy = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT IFNULL(SUM(merma_proceso_monotapas), 0)
                FROM registros_control
                WHERE DATE(fecha_registro) = CURDATE()
                  AND {filtroCalidad};
            ",
                    conn
                )
            )
            {
                result.MermaProcesoHoy = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT COUNT(*)
                FROM registros_control
                WHERE DATE(fecha_registro) = CURDATE()
                  AND observacion IS NOT NULL
                  AND TRIM(observacion) <> ''
                  AND {filtroCalidad};
            ",
                    conn
                )
            )
            {
                result.RegistrosConObservacionHoy = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    @"
                SELECT
                    rc.id,
                    DATE_FORMAT(rc.fecha_registro, '%d-%m-%Y') AS fecha,
                    TIME_FORMAT(rc.hora_registro, '%H:%i') AS hora,

                    IFNULL(u.nombre_completo, '-') AS usuario,
                    IFNULL(p.nombre, '-') AS proceso,
                    IFNULL(m.nombre, '-') AS maquina,
                    IFNULL(f.nombre, '-') AS formulario,

                    IFNULL(rc.np, '-') AS np,
                    IFNULL(rc.descripcion_producto, '-') AS producto,

                    IFNULL(rc.turno, '-') AS turno,
                    IFNULL(ec.nombre, '-') AS estado,
                    IFNULL(rc.observacion, '-') AS observacion

                FROM registros_control rc
                LEFT JOIN usuarios u ON rc.usuario_id = u.id
                LEFT JOIN procesos p ON rc.proceso_id = p.id
                LEFT JOIN maquinas m ON rc.maquina_id = m.id
                LEFT JOIN formularios_control f ON rc.formulario_id = f.id
                LEFT JOIN estados_catalogo ec ON rc.estado_id = ec.id

                WHERE UPPER(IFNULL(rc.area, '')) = 'CALIDAD'

                ORDER BY rc.id DESC
                LIMIT 10;
            ",
                    conn
                )
            )
            {
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.UltimosRegistros.Add(
                        new DashboardRegistroDto
                        {
                            Id = reader.GetInt32("id"),
                            FechaRegistro = reader.GetString("fecha"),
                            HoraRegistro = reader.GetString("hora"),
                            Usuario = reader.GetString("usuario"),
                            Proceso = reader.GetString("proceso"),
                            Maquina = reader.GetString("maquina"),
                            Formulario = reader.GetString("formulario"),
                            Np = reader.GetString("np"),
                            Producto = reader.GetString("producto"),
                            Turno = reader.GetString("turno"),
                            Estado = reader.GetString("estado"),
                            Observacion = reader.GetString("observacion"),
                        }
                    );
                }
            }

            return result;
        }
    }
}
