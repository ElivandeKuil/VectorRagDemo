using Microsoft.EntityFrameworkCore;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.DAL
{
    public class LogboekDbContext : DbContext
    {
        public LogboekDbContext(DbContextOptions<LogboekDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApiCallLog> ApiCallLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ApiCallLog configuration
            modelBuilder.Entity<ApiCallLog>(entity =>
            {
                entity.ToTable("ApiCallLog");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.RequestMethod).HasMaxLength(10).IsRequired();
                entity.Property(e => e.RequestUri).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.RequestHeaders).HasColumnType("VARCHAR(MAX)");
                entity.Property(e => e.RequestContent).HasColumnType("VARCHAR(MAX)");
                entity.Property(e => e.ResponseStatus).IsRequired();
                entity.Property(e => e.ResponseHeaders).HasColumnType("VARCHAR(MAX)");
                entity.Property(e => e.ResponseContent).HasColumnType("VARCHAR(MAX)");
                entity.Property(e => e.DurationMs);
                entity.Property(e => e.ErrorMessage).HasColumnType("VARCHAR(MAX)");
                entity.Property(e => e.CorrelationId);
                entity.Property(e => e.ClientIP).HasMaxLength(50);
                entity.Property(e => e.GemaaktOp).IsRequired().HasDefaultValueSql("GETDATE()");

                // Indexes
                entity.HasIndex(e => e.GemaaktOp).HasDatabaseName("IX_GemaaktOp");
                entity.HasIndex(e => e.ResponseStatus).HasDatabaseName("IX_ResponseStatus");
            });
        }
    }
}
