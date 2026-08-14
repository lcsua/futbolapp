using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Endpoint)
            .IsRequired()
            .HasMaxLength(2048)
            .HasColumnName("endpoint");

        builder.Property(e => e.P256dh)
            .IsRequired()
            .HasMaxLength(512)
            .HasColumnName("p256dh");

        builder.Property(e => e.Auth)
            .IsRequired()
            .HasMaxLength(512)
            .HasColumnName("auth");

        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
        builder.Property(e => e.UserAgent).HasMaxLength(512).HasColumnName("user_agent");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.Endpoint).IsUnique();
        builder.HasIndex(e => e.IsActive);

        builder.HasMany(e => e.Follows)
            .WithOne(f => f.PushSubscription)
            .HasForeignKey(f => f.PushSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}

public class PushFollowConfiguration : IEntityTypeConfiguration<PushFollow>
{
    public void Configure(EntityTypeBuilder<PushFollow> builder)
    {
        builder.ToTable("push_follows");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.PushSubscriptionId).HasColumnName("push_subscription_id");
        builder.Property(e => e.ScopeType)
            .HasConversion<int>()
            .HasColumnName("scope_type");
        builder.Property(e => e.ScopeId).HasColumnName("scope_id");

        builder.Property(e => e.NotifyResults).HasColumnName("notify_results");
        builder.Property(e => e.NotifyFixture).HasColumnName("notify_fixture");
        builder.Property(e => e.NotifyStandings).HasColumnName("notify_standings");
        builder.Property(e => e.NotifyNews).HasColumnName("notify_news");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.PushSubscriptionId, e.ScopeType, e.ScopeId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(e => new { e.ScopeType, e.ScopeId });

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
