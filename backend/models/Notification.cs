
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("AppMessages", Schema = "HumanResources")]
    public class Notification
    {
        [Key]
        public int ID { get; set; }

        public string Message { get; set; } = string.Empty;
        public int BusinessEntityID { get; set; }
    }
}
