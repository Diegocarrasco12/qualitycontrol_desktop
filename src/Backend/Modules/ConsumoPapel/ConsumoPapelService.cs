using LogisticControlCenter.Services;
using LogisticControlCenter.Repositories.ConsumoPapel;

namespace LogisticControlCenter.Modules.ConsumoPapel
{
    public class ConsumoPapelService
    {
        private readonly ConsumoPapelRepository _repository;

        public ConsumoPapelService(DbService db)
        {
            _repository = new ConsumoPapelRepository(db);
        }

        // =========================
        // LISTADO
        // =========================
        public async Task<ConsumoPapelPagedResult> ObtenerConsumos(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string codigo,
            string lote)
        {
            ValidarPaginacion(ref page, ref limit);

            return await _repository.ObtenerConsumos(
                page,
                limit,
                fechaDesde?.Trim() ?? "",
                fechaHasta?.Trim() ?? "",
                codigo?.Trim() ?? "",
                lote?.Trim() ?? ""
            );
        }

        // =========================
        // KPIS
        // =========================
        public async Task<ConsumoKpiItem> ObtenerKpis()
        {
            return await _repository.ObtenerKpis();
        }

        // =========================
        // GUARDAR CAMBIOS
        // =========================
        public async Task GuardarCambios(List<ConsumoCambioItem> cambios)
        {
            if (cambios == null || cambios.Count == 0)
                throw new Exception("No hay cambios válidos");

            foreach (var cambio in cambios)
            {
                if (cambio.Id <= 0)
                    throw new Exception($"ID inválido: {cambio.Id}");

                cambio.Estado = (cambio.Estado ?? "").Trim();
                cambio.Salida = (cambio.Salida ?? "").Trim();
            }

            try
            {
                await _repository.GuardarCambios(cambios);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR SERVICE: {ex}");
                throw; // 🔥 importante: deja que suba al handler
            }
        }

        // =========================
        // EXPORTACIÓN
        // =========================
        public async Task<List<ConsumoPapelItem>> ObtenerConsumosParaExportacion(
            string fechaDesde,
            string fechaHasta,
            string codigo,
            string lote)
        {
            var result = await ObtenerConsumos(
                page: 1,
                limit: 100000,
                fechaDesde: fechaDesde,
                fechaHasta: fechaHasta,
                codigo: codigo,
                lote: lote
            );

            return result.Items ?? new List<ConsumoPapelItem>();
        }

        // =========================
        // VALIDACIONES
        // =========================
        private void ValidarPaginacion(ref int page, ref int limit)
        {
            if (page <= 0)
                page = 1;

            if (limit <= 0)
                limit = 20;

            if (limit > 100000)
                limit = 100000;
        }
    }
}