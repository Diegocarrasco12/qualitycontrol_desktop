using System;
using System.Collections.Generic;
using LogisticControlCenter.Repositories.BinsLavado;
using LogisticControlCenter.Modules.BinsLavado;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.BinsLavado
{
    public class BinsLavadoService
    {
        private readonly BinsLavadoRepository _repo;

        public BinsLavadoService(DbService db)
        {
            _repo = new BinsLavadoRepository(db);
        }

        // =========================
        // OBTENER LAVADOS
        // =========================
        public (List<BinsLavadoItem> items, int total, int pages) ObtenerLavados(
            int page,
            int limit,
            string fechaDesde,
            string fechaHasta,
            string bin,
            string calle,
            string documento
        )
        {
            ValidarPaginacion(ref page, ref limit);

            return _repo.ObtenerLavados(
                page,
                limit,
                fechaDesde?.Trim() ?? "",
                fechaHasta?.Trim() ?? "",
                bin?.Trim() ?? "",
                calle?.Trim() ?? "",
                documento?.Trim() ?? ""
            );
        }

        // =========================
        // KPI HOY (🔥 NUEVO)
        // =========================
        public int ObtenerKPIHoy()
        {
            try
            {
                return _repo.ObtenerKPIHoy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR KPI LAVADO: {ex}");
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
    string documento
)
{
    try
    {
        return await _repo.ExportarExcel(
            fechaDesde?.Trim() ?? "",
            fechaHasta?.Trim() ?? "",
            bin?.Trim() ?? "",
            calle?.Trim() ?? "",
            documento?.Trim() ?? ""
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR SERVICE EXPORT LAVADO: {ex}");
        throw;
    }
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