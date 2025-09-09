using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using July2025Capstone.Models;

namespace July2025Capstone.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // DbSets for all entities
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<InsurancePolicy> InsurancePolicies { get; set; }
        public DbSet<EmergencyContact> EmergencyContacts { get; set; }
        public DbSet<Lifestyle> Lifestyles { get; set; }
        public DbSet<VisitIntake> VisitIntakes { get; set; }
        public DbSet<Condition> Conditions { get; set; }
        public DbSet<PatientCondition> PatientConditions { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<Procedure> Procedures { get; set; }
        public DbSet<Consent> Consents { get; set; }
        public DbSet<Pharmacy> Pharmacies { get; set; }
        public DbSet<VitalWeight> VitalWeights { get; set; }
        public DbSet<VitalBloodPressure> VitalBloodPressures { get; set; }
        public DbSet<VitalGlucose> VitalGlucoses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Patient relationships
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Address)
                .WithMany(a => a.Patients)
                .HasForeignKey(p => p.AddressId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint on Patient.UserId as shown in ERD
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // Insurance Policy relationships
            modelBuilder.Entity<InsurancePolicy>()
                .HasOne(ip => ip.Patient)
                .WithMany(p => p.InsurancePolicies)
                .HasForeignKey(ip => ip.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Emergency Contact relationships
            modelBuilder.Entity<EmergencyContact>()
                .HasOne(ec => ec.Patient)
                .WithMany(p => p.EmergencyContacts)
                .HasForeignKey(ec => ec.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lifestyle relationships (one-to-one)
            modelBuilder.Entity<Lifestyle>()
                .HasOne(l => l.Patient)
                .WithOne(p => p.Lifestyle)
                .HasForeignKey<Lifestyle>(l => l.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Visit Intake relationships
            modelBuilder.Entity<VisitIntake>()
                .HasOne(vi => vi.Patient)
                .WithMany(p => p.VisitIntakes)
                .HasForeignKey(vi => vi.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient Condition relationships (many-to-many)
            modelBuilder.Entity<PatientCondition>()
                .HasKey(pc => new { pc.PatientId, pc.ConditionId });

            modelBuilder.Entity<PatientCondition>()
                .HasOne(pc => pc.Patient)
                .WithMany(p => p.PatientConditions)
                .HasForeignKey(pc => pc.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientCondition>()
                .HasOne(pc => pc.Condition)
                .WithMany(c => c.PatientConditions)
                .HasForeignKey(pc => pc.ConditionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Medication relationships
            modelBuilder.Entity<Medication>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.Medications)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Allergy relationships
            modelBuilder.Entity<Allergy>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Allergies)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Procedure relationships
            modelBuilder.Entity<Procedure>()
                .HasOne(pr => pr.Patient)
                .WithMany(p => p.Procedures)
                .HasForeignKey(pr => pr.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Consent relationships (one-to-one)
            modelBuilder.Entity<Consent>()
                .HasOne(c => c.Patient)
                .WithOne(p => p.Consent)
                .HasForeignKey<Consent>(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Pharmacy relationships - CHANGED TO RESTRICT
            modelBuilder.Entity<Pharmacy>()
                .HasOne(ph => ph.Address)
                .WithMany(a => a.Pharmacies)
                .HasForeignKey(ph => ph.AddressId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to Restrict

            // Patient preferred pharmacy - NOW CAN USE SETNULL
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.PreferredPharmacy)
                .WithMany(ph => ph.Patients)
                .HasForeignKey("PreferredPharmacyId")
                .OnDelete(DeleteBehavior.SetNull); // Changed back to SetNull

            // Vital Signs relationships
            modelBuilder.Entity<VitalWeight>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(vw => vw.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VitalBloodPressure>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(vbp => vbp.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VitalGlucose>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(vg => vg.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision
            modelBuilder.Entity<Medication>()
                .Property(m => m.DosageStrength)
                .HasPrecision(10, 4);

            modelBuilder.Entity<VitalWeight>()
                .Property(vw => vw.WeightValue)
                .HasPrecision(6, 2);

            modelBuilder.Entity<VitalGlucose>()
                .Property(vg => vg.GlucoseValue)
                .HasPrecision(6, 2);
        }
    }
}
