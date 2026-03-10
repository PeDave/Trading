using LaBot.Api.Models.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LaBot.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TradingSignal> TradingSignals => Set<TradingSignal>();
    public DbSet<ChartAnalysis> ChartAnalyses => Set<ChartAnalysis>();
    public DbSet<Playbook> Playbooks => Set<Playbook>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<FeatureGate> FeatureGates => Set<FeatureGate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TradingSignal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Timeframe).HasMaxLength(10);
            entity.Property(e => e.EntryPrice).HasColumnType("decimal(18,8)");
            entity.Property(e => e.StopLoss).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TakeProfit1).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TakeProfit2).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TakeProfit3).HasColumnType("decimal(18,8)");
            entity.Property(e => e.RiskRewardRatio).HasColumnType("decimal(18,4)");
            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.CreatedSignals)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        builder.Entity<Playbook>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Symbol).HasMaxLength(20);
            entity.Property(e => e.Timeframe).HasMaxLength(10);
            entity.Property(e => e.Probability).HasColumnType("decimal(5,4)");
            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ChartAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ImagePath).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Playbook)
                  .WithMany(p => p.ChartAnalyses)
                  .HasForeignKey(e => e.PlaybookId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.ChartAnalyses)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(18,8)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,8)");
            entity.Property(e => e.FilledQuantity).HasColumnType("decimal(18,8)");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BitgetOrderId);
        });

        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentReference).HasMaxLength(200);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Subscriptions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
        });

        builder.Entity<FeatureGate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FeatureName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(450);
            entity.HasIndex(e => e.FeatureName).IsUnique();
        });

        // Rename Identity tables to use snake_case to match PostgreSQL conventions
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("user_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("user_tokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("role_claims");
    }
}
