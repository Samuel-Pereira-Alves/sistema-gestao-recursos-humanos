namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class PersonDto
    {
        public int BusinessEntityID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }   
        public string? Title { get; set; }        
        public string? Suffix { get; set; }       
    }

}