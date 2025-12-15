namespace sistema_gestao_recursos_humanos.backend.models.tools
{
    public record EmploymentItem(
        DateTime? StartDate,
        DateTime? EndDate,
        string OrgName,
        string JobTitle,
        string Responsibility,
        string FunctionCategory,
        string IndustryCategory,
        string CountryRegion,
        string State,
        string City
        );

    public class ResumeData
    {
        public string FirstName { get; set; } = "Unknown";
        public string LastName  { get; set; } = "Unknown";
        public string Email     { get; set; } = "unknown@example.com";
        public string Summary   { get; set; } = "N/A";
        public string Skills    { get; set; } = "N/A";
        public List<EmploymentItem> Employment { get; set; } = new();

        public string EduLevel  { get; set; } = "Bachelor";
        public DateTime? EduStart { get; set; } = new DateTime(2020,1,1);
        public DateTime? EduEnd   { get; set; } = new DateTime(2024,1,1);
        public string EduDegree { get; set; } = "Unknown";
        public string EduMajor  { get; set; } = "Unknown";
        public string EduMinor  { get; set; } = "Unknown";
        public string EduGPA    { get; set; } = "0.0";
        public string EduGPAScale { get; set; } = "4";
        public string EduSchool { get; set; } = "Unknown";
        public string CountryRegion { get; set; } = "N/A";
        public string State { get; set; } = "N/A";
        public string City  { get; set; } = "N/A";
        public string TelNumber { get; set; } = "0000000";
    }
}