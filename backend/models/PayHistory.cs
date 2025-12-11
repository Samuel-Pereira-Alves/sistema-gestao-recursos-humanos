using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("EmployeePayHistory", Schema = "HumanResources")]
public class PayHistory
{
    [Key]
    public int BusinessEntityID { get; set; }
    public DateTime RateChangeDate { get; set; }
    public decimal Rate { get; set; }
    public byte PayFrequency { get; set; }
    public DateTime ModifiedDate { get; set; }
}
}