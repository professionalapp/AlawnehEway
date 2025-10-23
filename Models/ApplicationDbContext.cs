using Microsoft.EntityFrameworkCore;

namespace AlawnehEway.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Add your DbSet properties here for your models, e.g.
        public DbSet<User> Users { get; set; }
        public DbSet<Party> Parties { get; set; }
        public DbSet<Remittance> Remittances { get; set; }
        public DbSet<RemittanceChangeRequest> RemittanceChangeRequests { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<FeeTier> FeeTiers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Remittance>()
                .HasOne(r => r.Sender)
                .WithMany()
                .HasForeignKey(r => r.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Remittance>()
                .HasOne(r => r.Beneficiary)
                .WithMany()
                .HasForeignKey(r => r.BeneficiaryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Remittance>()
                .HasIndex(r => r.Reference)
                .IsUnique(false);
        }
    }
}
