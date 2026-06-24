using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.EntityConfigurations;

public class TaskItemEntityConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(t => t.UserId);
               
        builder.Property(t => t.Status).HasConversion<string>();
        builder.Property(t => t.Priority).HasConversion<string>();
    }
}
