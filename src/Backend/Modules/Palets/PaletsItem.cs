namespace LogisticControlCenter.Modules.Palets
{
    public class PaletsItem
    {
        public int Id { get; set; }

  
        public string Fecha { get; set; } = "";

      
        public string Planta { get; set; } = "";

        public string TipoMovimiento { get; set; } = "";

        public string Ean13 { get; set; } = "";

        public string Detalle { get; set; } = "";

        public int Cantidad { get; set; }

        public string Lote { get; set; } = "";
    }
}