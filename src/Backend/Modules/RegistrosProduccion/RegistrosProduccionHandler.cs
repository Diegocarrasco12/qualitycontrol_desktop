using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using QualityControlCenter.Services;

namespace QualityControlCenter.Modules.RegistrosProduccion
{
    public class RegistrosProduccionHandler
    {
        private readonly RegistrosProduccionRepository _repository;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public RegistrosProduccionHandler(DbService db)
        {
            _repository = new RegistrosProduccionRepository(db);
        }

        public async Task<string> Handle(string action, Dictionary<string, object> data)
        {
            try
            {
                if (action == "registrosProduccion.obtenerResumen")
                {
                    var resumen = await _repository.ObtenerResumen();

                    return JsonSerializer.Serialize(
                        new { ok = true, data = resumen },
                        _jsonOptions
                    );
                }

                return JsonSerializer.Serialize(
                    new
                    {
                        ok = false,
                        error = $"Acción registros producción no reconocida: {action}",
                    },
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
