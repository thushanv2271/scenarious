namespace Saral.FileProcessor.Data.Context;

public class FileProcessorDbContext : DbContext
{
    public FileProcessorDbContext(DbContextOptions<FileProcessorDbContext> options)
        : base(options)
    {
    }

    public DbSet<FileValidationResult> FileValidationResults { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FileValidationResult entity to match SaralBackend schema
        modelBuilder.Entity<FileValidationResult>(entity =>
        {
            entity.ToTable("file_validation_results");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
                
            entity.Property(e => e.Filename)
                .HasColumnName("filename")
                .HasMaxLength(500)
                .IsRequired();
                
            entity.Property(e => e.TotalRows)
                .HasColumnName("total_rows");
                
            entity.Property(e => e.TotalErrors)
                .HasColumnName("total_errors");
                
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired();
                
            entity.Property(e => e.SessionId)
                .HasColumnName("session_id")
                .IsRequired();
                
            entity.Property(e => e.CreatedOnUtc)
                .HasColumnName("created_on_utc")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            entity.Property(e => e.ModifiedOnUtc)
                .HasColumnName("modified_on_utc")
                .HasColumnType("timestamptz");

            // Indexes for performance
            entity.HasIndex(e => e.Filename)
                .HasDatabaseName("ix_file_validation_results_filename");
                
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_file_validation_results_status");
                
            entity.HasIndex(e => e.CreatedOnUtc)
                .HasDatabaseName("ix_file_validation_results_created_on_utc");
        });
    }
}