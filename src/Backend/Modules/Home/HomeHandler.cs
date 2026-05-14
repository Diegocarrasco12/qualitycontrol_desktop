using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogisticControlCenter.Services;

namespace LogisticControlCenter.Modules.Home
{
    public class HomeHandler
    {
        private readonly HomeService _service;

        public HomeHandler(DbService db)
        {
            _service = new HomeService(db);
        }

        public async Task<string> Handle(string action, Dictionary<string, object>? data)
        {
            try
            {
                Console.WriteLine($"📥 ACTION: {action}");

                switch (action)
                {
                    case "inicio.getDashboard":
                        return await ObtenerDashboard();

                    default:
                        return Error($"Acción no reconocida: {action}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR HANDLER HOME: {ex}");
                return Error(ex.Message);
            }
        }

        // =========================
        // DASHBOARD
        // =========================
        private async Task<string> ObtenerDashboard()
{
    var total = await _service.GetDashboard();        // histórico
var semana = await _service.GetUltimos7Dias();    // últimos 7 días
var hoy = await _service.GetHoy();                // hoy

var actividad = await _service.ObtenerActividadReciente();
var alertas = await _service.ObtenerAlertas();

return Ok(new
{
    total = new {
        bins = total.bins,
        lavado = total.lavado,
        palets = total.palets,
        consumo = total.consumo,
        altillo = total.altillo
    },
    semana = new {
        bins = semana.bins,
        lavado = semana.lavado,
        palets = semana.palets,
        consumo = semana.consumo,
        altillo = semana.altillo
    },
    hoy = new {
        bins = hoy.bins,
        lavado = hoy.lavado,
        palets = hoy.palets,
        consumo = hoy.consumo,
        altillo = hoy.altillo
    },
    actividad = actividad,
    alertas = alertas
});
}

        // =========================
        // RESPUESTAS STANDARD
        // =========================
        private string Ok(object? data)
        {
            return JsonSerializer.Serialize(new
            {
                ok = true,
                data,
                error = (string?)null
            });
        }

        private string Error(string message)
        {
            return JsonSerializer.Serialize(new
            {
                ok = false,
                data = (object?)null,
                error = message
            });
        }
    }
}