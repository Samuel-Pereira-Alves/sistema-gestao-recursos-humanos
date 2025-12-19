
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
        public DbSet<Department> Departments { get; set; }
        public DbSet<PayHistory> PayHistories { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<SystemUser> SystemUsers { get; set; }
        public DbSet<BusinessEntity> BusinessEntities { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Log> Logs {get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // BusinessEntity (IDENTITY + triggers)
            modelBuilder.Entity<BusinessEntity>()
                .ToTable("BusinessEntity", "Person", tb =>
                {
                    tb.HasTrigger("TR_BusinessEntity_Insert");
                    tb.HasTrigger("TR_BusinessEntity_Update");
                    tb.HasTrigger("TR_BusinessEntity_Delete");
                })
                .HasKey(be => be.BusinessEntityID);

            modelBuilder.Entity<BusinessEntity>()
                .Property(be => be.BusinessEntityID)
                .ValueGeneratedOnAdd(); // IDENTITY

            modelBuilder.Entity<BusinessEntity>()
                .Property(be => be.RowGuid)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<BusinessEntity>()
                .Property(be => be.ModifiedDate)
                .HasDefaultValueSql("GETDATE()");

            // -----------------------------
            // Employee
            // -----------------------------
            modelBuilder.Entity<Employee>()
                .ToTable("Employee", "HumanResources", tb =>
                {
                    // Informe que a tabela tem triggers (nomes exemplificativos)
                    tb.HasTrigger("TR_Employee_Insert");
                    tb.HasTrigger("TR_Employee_Update");
                    tb.HasTrigger("TR_Employee_Delete");
                })
                .HasKey(e => e.BusinessEntityID);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Person)
                .WithOne(p => p.Employee)
                .HasForeignKey<Employee>(e => e.BusinessEntityID);


            modelBuilder.Entity<Employee>()
                    .Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("GETDATE()");

            // -----------------------------
            // Person
            // -----------------------------
            modelBuilder.Entity<Person>()
                .ToTable("Person", "Person", tb =>
                {
                    tb.HasTrigger("TR_Person_Insert");
                    tb.HasTrigger("TR_Person_Update");
                    tb.HasTrigger("TR_Person_Delete");
                })
                .HasKey(p => p.BusinessEntityID);

            

            // -----------------------------
            // PayHistory
            // -----------------------------
            modelBuilder.Entity<PayHistory>()
                .ToTable("EmployeePayHistory", "HumanResources")
                .HasKey(ph => new { ph.BusinessEntityID, ph.RateChangeDate });

            modelBuilder.Entity<PayHistory>()
                .HasOne(ph => ph.Employee)
                .WithMany(e => e.PayHistories)
                .HasForeignKey(ph => ph.BusinessEntityID);


            modelBuilder.Entity<PayHistory>()
                    .Property(ph => ph.Rate)
                    .HasPrecision(19, 4);

            // -----------------------------
            // DepartmentHistory
            // -----------------------------
            modelBuilder.Entity<DepartmentHistory>()
                .ToTable("EmployeeDepartmentHistory", "HumanResources")
                .HasKey(dh => new { dh.BusinessEntityID, dh.DepartmentID, dh.ShiftID, dh.StartDate });

            modelBuilder.Entity<DepartmentHistory>()
                .HasOne(dh => dh.Employee)
                .WithMany(e => e.DepartmentHistories)
                .HasForeignKey(dh => dh.BusinessEntityID);

            modelBuilder.Entity<DepartmentHistory>()
                .HasOne(dh => dh.Department)
                .WithMany(d => d.DepartmentHistories)
                .HasForeignKey(dh => dh.DepartmentID);

            modelBuilder.Entity<Department>()
                .ToTable("Department", "HumanResources")
                .HasKey(d => d.DepartmentID);

            // -----------------------------
            // SystemUser
            // -----------------------------
            modelBuilder.Entity<SystemUser>()
                .ToTable("SystemUsers", "HumanResources", tb =>
                {
                    tb.HasTrigger("TR_SystemUsers_Insert");
                    tb.HasTrigger("TR_SystemUsers_Update");
                    tb.HasTrigger("TR_SystemUsers_Delete");
                })
                .HasKey(su => su.SystemUserId);

            // FK SystemUser -> Employee (BusinessEntityID)
            modelBuilder.Entity<SystemUser>()
                .HasOne<Employee>()
                .WithOne()
                .HasForeignKey<SystemUser>(su => su.BusinessEntityID);

            // Índices/constraints conforme a tua tabela SQL
            modelBuilder.Entity<SystemUser>()
                .HasIndex(su => su.BusinessEntityID)
                .IsUnique();

            modelBuilder.Entity<SystemUser>()
                .HasIndex(su => su.Username)
                .IsUnique();
        }
    }
}
