using System.Collections.Generic;
using LogisticControlCenter.Repositories.Palets;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.Palets
{
    public class PaletsService
    {
        private readonly PaletsRepository _repo;

        public PaletsService(DbService db)
        {
            _repo = new PaletsRepository(db);
        }

        // =========================
        // LISTADO
        // =========================
        public (List<PaletsItem> items, int total, int pages) ObtenerRegistros(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string lote,
            string planta,
            string tipo
        )
        {
            return _repo.ObtenerRegistros(page, limit, fechaDesde, fechaHasta, lote, planta, tipo);
        }

        // =========================
        // KPI HOY
        // =========================
        public int ObtenerKPIHoy()
        {
            return _repo.ObtenerKPIHoy();
        }

        // =========================
        // KPI ÚLTIMO DÍA
        // =========================
        public int ObtenerKPIUltimoDia()
        {
            return _repo.ObtenerKPIUltimoDia();
        }

        // =========================
        // EXPORTAR EXCEL
        // =========================
        public async Task<string> ExportarExcel(
            string fechaDesde,
            string fechaHasta,
            string lote,
            string planta,
            string tipo
        )
        {
            return await _repo.ExportarExcel(
                fechaDesde?.Trim() ?? "",
                fechaHasta?.Trim() ?? "",
                lote?.Trim() ?? "",
                planta?.Trim() ?? "",
                tipo?.Trim() ?? ""
            );
        }
    }
}
