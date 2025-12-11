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
                .HasMany(e => e.PayHistories)
                .WithOne()
                .HasForeignKey(ph => ph.EmployeeId);

            modelBuilder.Entity<Employee>()
                .HasMany(e => e.DepartmentHistories)
                .WithOne()
                .HasForeignKey(dh => dh.EmployeeId);
        }
    }
}