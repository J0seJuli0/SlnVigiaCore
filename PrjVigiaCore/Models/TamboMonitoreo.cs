using System.ComponentModel.DataAnnotations;

namespace PrjVigiaCore.Models
{
    public class TamboMonitoreo
    {
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        public string Fecha { get; set; }

        [Required(ErrorMessage = "La hora es obligatoria")]
        [DataType(DataType.Time)]
        public string Hora { get; set; }

        [Required(ErrorMessage = "El destinatario es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        public string Destinatario { get; set; }

        [Required(ErrorMessage = "El responsable es obligatorio")]
        public string Responsable { get; set; }

        public string Server1Estado { get; set; }
        public string Server1Fecha { get; set; }
        public string Server1Disco { get; set; }
        public string Server1NFS { get; set; }
        public string Server1CPU { get; set; }
        public string Server1Servicios { get; set; }
        public string? Server1Comentario { get; set; }

        public string Server2Estado { get; set; }
        public string Server2Fecha { get; set; }
        public string Server2Disco { get; set; }
        public string Server2NFS { get; set; }
        public string Server2CPU { get; set; }
        public string Server2Servicios { get; set; }
        public string? Server2Comentario { get; set; }

        public string Server3Estado { get; set; }
        public string Server3Fecha { get; set; }
        public string Server3Disco { get; set; }
        public string Server3CPU { get; set; }
        public string? Server3Comentario { get; set; }
    }
}
