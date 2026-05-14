using LogisticControlCenter.Repositories.Paletizado;

namespace LogisticControlCenter.Services
{
    public class PaletizadoService
    {
        private readonly PaletizadoRepository _repository;

        public PaletizadoService(PaletizadoRepository repository)
        {
            _repository = repository;
        }

        // =========================
        // LISTADO
        // =========================
        public async Task<object> ObtenerPalets(
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
            var result = await _repository.ObtenerPalets(
                page,
                limit,
                fechaDesde,
                fechaHasta,
                planta,
                taller,
                np,
                idPalet
            );

            return new
            {
                items = result.Items,
                total = result.Total,
                pages = result.Pages
            };
        }
    }
}