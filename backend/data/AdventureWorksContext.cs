using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.models;

namespace sistema_gestao_recursos_humanos.backend.data
{
    public class AdventureWorksContext : DbContext
    {
        public AdventureWorksContext(DbContextOptions<AdventureWorksContext> options)
            : base(options)
        {
        }

        public DbSet<JobCandidate> JobCandidates { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<DepartmentHistory> DepartmentHistories { get; set; }
        public DbSet<PayHistory> PayHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>()
                .ToTable("Employee", "HumanResources")
                .HasKey(e => e.BusinessEntityID);

            modelBuilder.Entity<PayHistory>()
                .ToTable("EmployeePayHistory", "HumanResources")
                .HasKey(ph => new { ph.BusinessEntityID, ph.RateChangeDate });

            modelBuilder.Entity<PayHistory>()
                .HasOne(ph => ph.Employee)
                .WithMany(e => e.PayHistories)
                .HasForeignKey(ph => ph.BusinessEntityID);

            // DepartmentHistory
            modelBuilder.Entity<DepartmentHistory>()
                .ToTable("EmployeeDepartmentHistory", "HumanResources")
                .HasKey(dh => new { dh.BusinessEntityID, dh.DepartmentID, dh.ShiftID, dh.StartDate });

            modelBuilder.Entity<DepartmentHistory>()
                .HasOne<Employee>()
                .WithMany(e => e.DepartmentHistories)
                .HasForeignKey(dh => dh.BusinessEntityID);
        }
    }
}