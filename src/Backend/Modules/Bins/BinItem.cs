namespace LogisticControlCenter.Modules.Bins
{
    // =========================
    // ITEM PRINCIPAL (TABLA)
    // =========================
    public class BinsItem
    {
        public int Id { get; set; }

        // Mantener string (ya lo hiciste bien)
        public string Fecha { get; set; } = "";

        public int NumeroBin { get; set; }

        public string Documento { get; set; } = "";

        public string Tipo { get; set; } = "";

        // 🔥 NUEVOS CAMPOS (del PHP real)
        public string Proveedor { get; set; } = "";

        public string EstadoBin { get; set; } = "";

        public string BinCodigo { get; set; } = "";

        public string Calle { get; set; } = "";

        public string Archivo { get; set; } = "";
    }

    // =========================
    // CAMBIOS (UPDATE)
    // =========================
    public class BinsCambioItem
    {
        public int Id { get; set; }

        public string Documento { get; set; } = "";

        public string EstadoBin { get; set; } = "";
    }

    // =========================
    // KPIs (simple, sin overkill)
    // =========================
    public class BinsKpiItem
    {
        public int TotalMovimientos { get; set; }

        public int EntradasHoy { get; set; }

        public int SalidasHoy { get; set; }

        public string UltimoBin { get; set; } = "";
    }

    // =========================
    // PAGINADO (igual a Consumo)
    // =========================
    public class BinsPagedResult
    {
        public List<BinsItem> Items { get; set; } = new();

        public int Total { get; set; }

        public int Pages { get; set; }
    }
}