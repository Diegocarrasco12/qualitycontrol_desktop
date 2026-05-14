namespace LogisticControlCenter.Modules.Altillo
{
    public class AltilloItem
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Codigo { get; set; } = string.Empty;

        public decimal Consumo { get; set; }

        public decimal UnidadesTarja { get; set; }

        public decimal Saldo { get; set; }

        public string NP { get; set; } = string.Empty;

        public string Lote { get; set; } = string.Empty;

        // 🔴 EDITABLES
        public string Comentario { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string ExtraPostEstado { get; set; } = string.Empty;

        // 🔵 CONTROL
        public bool Salida { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}