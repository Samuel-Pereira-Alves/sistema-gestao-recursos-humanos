using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class JobCandidateCreateForm
{
    public IFormFile Cv { get; set; } = default!;

    public DateTime BirthDate { get; set; }

    public string NationalIDNumber { get; set; } = string.Empty;

    public string MaritalStatus { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    [StringLength(64)]
    public string Role { get; set; } = string.Empty;

    // Opcional: BusinessEntityId (se existir ligação com outra entidade)
    public int? BusinessEntityId { get; set; }
}

