namespace LogisticControlCenter.Modules.BinsLavado
{
    public class BinsLavadoItem
    {
        public int Id { get; set; }

    
        public string Fecha { get; set; } = string.Empty;

        public int NumeroBin { get; set; }

        public string Documento { get; set; } = string.Empty;

        public string Proveedor { get; set; } = string.Empty;

        public string EstadoBin { get; set; } = string.Empty;

        public string BinCodigo { get; set; } = string.Empty;

        public string Calle { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public string Archivo { get; set; } = string.Empty;
    }
}