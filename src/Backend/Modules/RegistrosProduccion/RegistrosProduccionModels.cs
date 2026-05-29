using System.Collections.Generic;

namespace QualityControlCenter.Modules.RegistrosProduccion
{
    public class RegistrosProduccionResumenDto
    {
        public int TotalRegistros { get; set; }
        public int RegistrosHoy { get; set; }
        public int MaquinasConRegistros { get; set; }
        public int Rechazos { get; set; }

        public List<RegistroProduccionDto> Registros { get; set; } = new();
    }

    public class RegistroProduccionDto
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
