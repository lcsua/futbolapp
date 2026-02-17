using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class FieldBlackoutConfiguration : IEntityTypeConfiguration<FieldBlackout>
    {
        public void Configure(EntityTypeBuilder<FieldBlackout> builder)
        {
            builder.ToTable("field_blackouts");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.FieldId).HasColumnName("field_id");
            builder.Property(e => e.Date).HasColumnName("date");
            builder.Property(e => e.StartTime).HasColumnName("start_time");
            builder.Property(e => e.EndTime).HasColumnName("end_time");
            builder.Property(e => e.Reason).HasColumnName("reason");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(e => e.Field)
                .WithMany()
                .HasForeignKey(e => e.FieldId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
