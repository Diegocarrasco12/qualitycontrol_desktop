namespace LogisticControlCenter.Modules.ConsumoPapel
{
    public class ConsumoPapelItem
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public string Descripcion { get; set; } = "";

        public string Codigo { get; set; } = "";

        public decimal ConsumoKg { get; set; }

        public string NP { get; set; } = "";

        public decimal TarjaKg { get; set; }

        public decimal SaldoKg { get; set; }

        public string Lote { get; set; } = "";

        public string UbicacionBin { get; set; } = "";

        public string Estado { get; set; } = "";

        public string Salida { get; set; } = "";
    }

    public class ConsumoKpiItem
    {
        public decimal ConsumoHoy { get; set; }

        public int TarjasHoy { get; set; }

        public decimal SaldoTotal { get; set; }

        public string UltimoCodigo { get; set; } = "";

        public string UltimoLote { get; set; } = "";
    }

    public class ConsumoCambioItem
    {
        public int Id { get; set; }

        public string Estado { get; set; } = "";

        public string Salida { get; set; } = "";
    }

    public class ConsumoPapelPagedResult
    {
        public List<ConsumoPapelItem> Items { get; set; } = new();

        public int Total { get; set; }

        public int Pages { get; set; }
    }
}