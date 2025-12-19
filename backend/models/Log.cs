
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
        public DateTime Date { get; set; }
    }
}
