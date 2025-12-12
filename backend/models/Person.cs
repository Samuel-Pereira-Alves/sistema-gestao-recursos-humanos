using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("Person", Schema = "Person")]
    public class Person
    {
        [Key]
        public int BusinessEntityID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? Title { get; set; }
        public string? Suffix { get; set; }
        public int EmailPromotion { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string PersonType { get; set; } = "EM";

        // Navigation properties
        public Employee? Employee { get; set; }
    }
}