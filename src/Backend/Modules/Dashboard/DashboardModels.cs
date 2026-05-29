using System.Collections.Generic;

namespace QualityControlCenter.Modules.Dashboard
{
    public class DashboardResumenDto
    {
        public int ControlesHoy { get; set; }
        public decimal MermaInsumosHoy { get; set; }
        public decimal MermaProcesoHoy { get; set; }
        public int RegistrosConObservacionHoy { get; set; }

        public List<DashboardRegistroDto> UltimosRegistros { get; set; } = new();
    }

    public class DashboardRegistroDto
    {
        public int Id { get; set; }

        public string FechaRegistro { get; set; } = "";
        public string HoraRegistro { get; set; } = "";

        public string Usuario { get; set; } = "";
        public string Proceso { get; set; } = "";
        public string Maquina { get; set; } = "";
        public string Formulario { get; set; } = "";

        public string Np { get; set; } = "";
        public string Producto { get; set; } = "";

        public string Turno { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Observacion { get; set; } = "";
    }
}
