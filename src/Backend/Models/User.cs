using System;

namespace LogisticControlCenter.Models
{
    public class User
    {
        public int Id { get; set; }

        public string CodigoUsuario { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Rol { get; set; } = "usuario";

        public bool Activo { get; set; } = true;

        public DateTime CreadoEn { get; set; }

        public DateTime ActualizadoEn { get; set; }
    }
}
