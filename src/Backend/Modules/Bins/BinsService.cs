using LogisticControlCenter.Services;
using LogisticControlCenter.Repositories.Bins;

namespace LogisticControlCenter.Modules.Bins
{
    public class BinsService
    {
        private readonly BinsRepository _repository;

        public BinsService(DbService db)
        {
            _repository = new BinsRepository(db);
        }

        // =========================
        // LISTADO PAGINADO
        // =========================
        public async Task<BinsPagedResult> ObtenerRegistros(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string bin,
            string calle,
            string tipo,
            string documento)
        {
            ValidarPaginacion(ref page, ref limit);

            return await _repository.ObtenerRegistros(
                page,
                limit,
                fechaDesde?.Trim() ?? "",
                fechaHasta?.Trim() ?? "",
                bin?.Trim() ?? "",
                calle?.Trim() ?? "",
                tipo?.Trim() ?? "",
                documento?.Trim() ?? ""
            );
        }

        // =========================
// KPI HOY
// =========================
public async Task<(int entradas, int salidas)> ObtenerKPIHoy()
{
    return await _repository.ObtenerKPIHoy();
}

        // =========================
        // GUARDAR CAMBIOS
        // =========================
        public async Task GuardarCambios(List<BinsCambioItem> cambios)
        {
            if (cambios == null || cambios.Count == 0)
                throw new Exception("No hay cambios válidos");

            foreach (var c in cambios)
            {
                if (c.Id <= 0)
                    throw new Exception($"ID inválido: {c.Id}");

                c.Documento = (c.Documento ?? "").Trim();
                c.EstadoBin = (c.EstadoBin ?? "").Trim();
            }

            try
            {
                await _repository.GuardarCambios(cambios);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR SERVICE BINS: {ex}");
                throw;
            }
        }
        // =========================
// EXPORTAR EXCEL
// =========================
public async Task<string> ExportarExcel(
    string fechaDesde,
    string fechaHasta,
    string bin,
    string calle,
    string tipo,
    string documento)
{
    return await _repository.ExportarExcel(
        fechaDesde?.Trim() ?? "",
        fechaHasta?.Trim() ?? "",
        bin?.Trim() ?? "",
        calle?.Trim() ?? "",
        tipo?.Trim() ?? "",
        documento?.Trim() ?? ""
    );
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