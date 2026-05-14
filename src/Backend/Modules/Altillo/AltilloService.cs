using System;
using System.Collections.Generic;
using LogisticControlCenter.Repositories.Altillo;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.Altillo
{
    public class AltilloService
    {
        private readonly AltilloRepository _repo;

        public AltilloService(DbService db)
        {
            _repo = new AltilloRepository(db);
        }

        // =========================
        // OBTENER REGISTROS
        // =========================
        public (List<AltilloItem>, int) ObtenerRegistros(
            string? fechaDesde,
            string? fechaHasta,
            string? codigo,
            string? lote,
            int page,
            int limit
        )
        {
            return _repo.ObtenerRegistros(
                fechaDesde,
                fechaHasta,
                codigo,
                lote,
                page,
                limit
            );
        }

        // =========================
        // GUARDAR CAMBIOS
        // =========================
        public void GuardarCambios(List<AltilloCambioItem> cambios)
        {
            _repo.GuardarCambios(cambios);
        }

        // =========================
        // EXPORTAR
        // =========================
        public object ExportarExcel(
            string? fechaDesde,
            string? fechaHasta,
            string? codigo,
            string? lote
        )
        {
            return _repo.ExportarExcel(
                fechaDesde,
                fechaHasta,
                codigo,
                lote
            );
        }
        // =========================
// KPIS
// =========================
public object ObtenerKpis()
{
    return _repo.ObtenerKpis();
}
    }
}