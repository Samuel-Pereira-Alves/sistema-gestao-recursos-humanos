using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("Department", Schema = "HumanResources")]
    public class Department
    {
        [Key]
        public short DepartmentID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }

        public List<DepartmentHistory>? DepartmentHistories { get; set; }
    }
}