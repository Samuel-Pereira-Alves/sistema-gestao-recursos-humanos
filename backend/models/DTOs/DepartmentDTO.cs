namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class DepartmentDto
    {
        public short DepartmentID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
    }
}