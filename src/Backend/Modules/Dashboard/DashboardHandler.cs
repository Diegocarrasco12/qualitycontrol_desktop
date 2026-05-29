using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using QualityControlCenter.Services;

namespace QualityControlCenter.Modules.Dashboard
{
    public class DashboardHandler
    {
        private readonly DashboardRepository _repository;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public DashboardHandler(DbService db)
        {
            _repository = new DashboardRepository(db);
        }

        public async Task<string> Handle(string action, Dictionary<string, object> data)
        {
            try
            {
                if (action == "dashboard.obtenerResumen")
                {
                    var resumen = await _repository.ObtenerResumen();

                    return JsonSerializer.Serialize(
                        new { ok = true, data = resumen },
                        _jsonOptions
                    );
                }

                return JsonSerializer.Serialize(
                    new { ok = false, error = $"Acción dashboard no reconocida: {action}" },
                    _jsonOptions
                );
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(
                    new { ok = false, error = ex.Message },
                    _jsonOptions
                );
            }
        }
    }
}
