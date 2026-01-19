using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class JobCandidateCreateForm
{
    [FromForm(Name = "cv")]
    public IFormFile Cv { get; set; } = default!;

    [FromForm(Name = "BirthDate")]
    public DateTime BirthDate { get; set; }

    public string NationalIDNumber { get; set; } = string.Empty;
    [FromForm(Name = "MaritalStatus")]

    public string MaritalStatus { get; set; } = string.Empty;
    [FromForm(Name = "Gender")]

    public string Gender { get; set; } = string.Empty;

    [FromForm(Name = "FirstName")]
    public string FirstName { get; set; } = string.Empty;
    [FromForm(Name = "LastName")]

    public string LastName { get; set; } = string.Empty;
    public string? Email {get; set;}

    [StringLength(64)]
    public string Role { get; set; } = string.Empty;

    // Opcional: BusinessEntityId (se existir ligação com outra entidade)
    public int? BusinessEntityId { get; set; }
}

