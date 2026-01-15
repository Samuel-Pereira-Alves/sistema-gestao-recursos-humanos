using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("Logs", Schema = "HumanResources")]
    public class Log
    {
        [Key]
        public int ID { get; set; }

        public string Message { get; set; } = string.Empty;
        public int? BusinessEntityID { get; set; }
        public LogType Type { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public DateTime Date { get; set; }
    }
}
