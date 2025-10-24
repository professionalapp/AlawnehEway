using Microsoft.EntityFrameworkCore;

namespace AlawnehEway.Models
{
    public class FxDbContext : DbContext
    {
        public FxDbContext(DbContextOptions<FxDbContext> options) : base(options)
        {
        }

        public DbSet<FxExchangeRate> FxExchangeRates { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // فهرس فريد على العملة
            modelBuilder.Entity<FxExchangeRate>()
                .HasIndex(fx => fx.Currency)
                .IsUnique();

            // علاقة مع المستخدم (الصندوق)
            modelBuilder.Entity<FxExchangeRate>()
                .HasOne(fx => fx.Cashier)
                .WithMany()
                .HasForeignKey(fx => fx.CashierId)
                .OnDelete(DeleteBehavior.SetNull);

            // إعداد جدول المستخدمين
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(50);
                entity.Property(e => e.PasswordHash).HasMaxLength(255);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Role).HasMaxLength(20);
            });
        }
    }
}
