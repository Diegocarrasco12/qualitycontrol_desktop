using MySqlConnector;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.Home
{
    public class HomeService
    {
        private readonly DbService _db;

        public HomeService(DbService db)
        {
            _db = db;
        }

        // =========================
        // TOTAL HISTÓRICO (NO TOCAR)
        // =========================
        public async Task<(int bins, int lavado, int palets, int consumo, int altillo)> GetDashboard()
        {
            int bins = 0;
            int lavado = 0;
            int palets = 0;
            int consumo = 0;
            int altillo = 0;

            try
            {
                using (var conn = _db.GetBinsConnection())
                {
                    await conn.OpenAsync();

                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM movimientos_bins", conn))
                        bins = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM movimientos_bins WHERE tipo = 'LAVADO'", conn))
                        lavado = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                }

                using (var conn = _db.GetConsumoPapelConnection())
                {
                    await conn.OpenAsync();

                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM tarjas_scan", conn))
                        consumo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM palets_movimientos", conn))
                        palets = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM altillo_scan", conn))
                        altillo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR HOME SERVICE: {ex}");
            }

            return (bins, lavado, palets, consumo, altillo);
        }

        // =========================
        // ÚLTIMOS 7 DÍAS
        // =========================
        public async Task<(int bins, int lavado, int palets, int consumo, int altillo)> GetUltimos7Dias()
        {
            int bins = 0, lavado = 0, palets = 0, consumo = 0, altillo = 0;

            using (var conn = _db.GetConsumoPapelConnection())
            {
                await conn.OpenAsync();

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM tarjas_scan WHERE fecha >= NOW() - INTERVAL 7 DAY", conn))
                    consumo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM palets_movimientos WHERE fecha >= NOW() - INTERVAL 7 DAY", conn))
                    palets = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM altillo_scan WHERE fecha >= NOW() - INTERVAL 7 DAY", conn))
                    altillo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            }

            using (var conn = _db.GetBinsConnection())
            {
                await conn.OpenAsync();

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM movimientos_bins WHERE fecha >= NOW() - INTERVAL 7 DAY", conn))
                    bins = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM movimientos_bins WHERE tipo='LAVADO' AND fecha >= NOW() - INTERVAL 7 DAY", conn))
                    lavado = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            }

            return (bins, lavado, palets, consumo, altillo);
        }

        // =========================
        // HOY
        // =========================
        public async Task<(int bins, int lavado, int palets, int consumo, int altillo)> GetHoy()
        {
            int bins = 0, lavado = 0, palets = 0, consumo = 0, altillo = 0;

            using (var conn = _db.GetConsumoPapelConnection())
            {
                await conn.OpenAsync();

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM tarjas_scan WHERE DATE(fecha) = CURDATE()", conn))
                    consumo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM palets_movimientos WHERE DATE(fecha) = CURDATE()", conn))
                    palets = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM altillo_scan WHERE DATE(fecha) = CURDATE()", conn))
                    altillo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            }

            using (var conn = _db.GetBinsConnection())
            {
                await conn.OpenAsync();

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM movimientos_bins WHERE DATE(fecha) = CURDATE()", conn))
                    bins = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM movimientos_bins WHERE tipo='LAVADO' AND DATE(fecha)=CURDATE()", conn))
                    lavado = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            }

            return (bins, lavado, palets, consumo, altillo);
        }

        // =========================
        // ACTIVIDAD
        // =========================
        public async Task<List<object>> ObtenerActividadReciente()
        {
            var lista = new List<object>();

            try
            {
                using (var conn = _db.GetConsumoPapelConnection())
                {
                    await conn.OpenAsync();

                    var sql = @"
                        SELECT descripcion, fecha
                        FROM tarjas_scan
                        ORDER BY fecha DESC
                        LIMIT 5
                    ";

                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new
                            {
                                descripcion = reader["descripcion"]?.ToString() ?? "",
                                fecha = reader["fecha"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR ACTIVIDAD: {ex}");
            }

            return lista;
        }

        // =========================
        // ALERTAS
        // =========================
        public async Task<List<string>> ObtenerAlertas()
        {
            var alertas = new List<string>();

            try
            {
                using (var conn = _db.GetConsumoPapelConnection())
                {
                    await conn.OpenAsync();

                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM tarjas_scan WHERE lote IS NULL OR lote = ''", conn))
                    {
                        var sinLote = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                        if (sinLote > 0)
                            alertas.Add($"⚠ {sinLote} registros sin lote");
                    }

                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM tarjas_scan WHERE codigo IS NULL OR codigo = ''", conn))
                    {
                        var sinCodigo = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                        if (sinCodigo > 0)
                            alertas.Add($"⚠ {sinCodigo} registros sin código");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR ALERTAS: {ex}");
            }

            return alertas;
        }
    }
}