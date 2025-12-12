using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("Employee", Schema = "HumanResources")]

    public class Employee
    {
        [Key]
        public int BusinessEntityID { get; set; }

        public string NationalIDNumber { get; set; } = string.Empty;
        public string LoginID { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string MaritalStatus { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public bool SalariedFlag { get; set; }
        public short VacationHours { get; set; }
        public short SickLeaveHours { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Navigation properties
        public List<DepartmentHistory> DepartmentHistories { get; set; } = new();
        public List<PayHistory> PayHistories { get; set; } = new();

        public Person? Person { get; set; }
    }

}