public class JobCandidate
{
    public int JobCandidateId { get; set; }
    public int? EmployeeId { get; set; }
    public string Resume { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
}