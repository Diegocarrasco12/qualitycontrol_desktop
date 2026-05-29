using System;
using System.Threading.Tasks;
using MySqlConnector;
using QualityControlCenter.Services;

namespace QualityControlCenter.Modules.RegistrosProduccion
{
    public class RegistrosProduccionRepository
    {
        private readonly DbService _db;

        public RegistrosProduccionRepository(DbService db)
        {
            _db = db;
        }

        public async Task<RegistrosProduccionResumenDto> ObtenerResumen()
        {
            var result = new RegistrosProduccionResumenDto();

            using var conn = _db.GetCalidadConnection();
            await conn.OpenAsync();

            var filtroProduccion = @"
    UPPER(IFNULL(rc.area, '')) = 'PRODUCCION'
";

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT COUNT(*)
                FROM registros_control rc
                WHERE {filtroProduccion};
            ",
                    conn
                )
            )
            {
                result.TotalRegistros = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT COUNT(*)
                FROM registros_control rc
                WHERE DATE(rc.fecha_registro) = CURDATE()
                  AND {filtroProduccion};
            ",
                    conn
                )
            )
            {
                result.RegistrosHoy = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT COUNT(DISTINCT rc.maquina_id)
                FROM registros_control rc
                WHERE rc.maquina_id IS NOT NULL
                  AND {filtroProduccion};
            ",
                    conn
                )
            )
            {
                result.MaquinasConRegistros = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    $@"
                SELECT COUNT(*)
                FROM registros_control rc
                LEFT JOIN estados_catalogo ec ON rc.estado_id = ec.id
                WHERE LOWER(IFNULL(ec.nombre, '')) = 'rechazado'
                  AND {filtroProduccion};
            ",
                    conn
                )
            )
            {
                result.Rechazos = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (
                var cmd = new MySqlCommand(
                    $@"
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

                WHERE {filtroProduccion}

                ORDER BY rc.id DESC
                LIMIT 300;
            ",
                    conn
                )
            )
            {
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Registros.Add(
                        new RegistroProduccionDto
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
