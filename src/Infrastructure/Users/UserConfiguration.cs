using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.BranchId).IsRequired(false); // Nullable
        builder.HasOne(u => u.Branch)
            .WithMany()
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.Restrict) // Prevent deleting branch if users exist
            .HasConstraintName("fk_users_branches_branch_id");
            
        builder.HasIndex(u => u.BranchId)
            .HasDatabaseName("ix_users_branch_id");
    }
}
