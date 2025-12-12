using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("SystemUsers", Schema = "HumanResources")]
    public class SystemUser
    {
        [Key]
        public int SystemUserId { get; set; }
        public int BusinessEntityID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

    }
}