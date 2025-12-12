
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("BusinessEntity", Schema = "Person")]
    public class BusinessEntity
    {
        [Key]
        public int BusinessEntityID { get; set; }

        public Guid RowGuid { get; set; } = Guid.NewGuid();
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }
}
