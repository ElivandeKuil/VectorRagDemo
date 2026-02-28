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
        public DbSet<AppLog> AppLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AppLog configuration
            modelBuilder.Entity<AppLog>(entity =>
            {
                entity.ToTable("applog");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.Level).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(200);
                entity.Property(e => e.Message).HasColumnType("text").IsRequired();
                entity.Property(e => e.Detail).HasColumnType("text");
                entity.Property(e => e.GemaaktOp).IsRequired().HasDefaultValueSql("NOW()");

                entity.HasIndex(e => e.GemaaktOp).HasDatabaseName("IX_AppLog_GemaaktOp");
                entity.HasIndex(e => e.Level).HasDatabaseName("IX_AppLog_Level");
            });

            // ApiCallLog configuration
            modelBuilder.Entity<ApiCallLog>(entity =>
            {
                entity.ToTable("apicalllog");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.RequestMethod).HasMaxLength(10).IsRequired();
                entity.Property(e => e.RequestUri).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.RequestHeaders).HasColumnType("text");
                entity.Property(e => e.RequestContent).HasColumnType("text");
                entity.Property(e => e.ResponseStatus).IsRequired();
                entity.Property(e => e.ResponseHeaders).HasColumnType("text");
                entity.Property(e => e.ResponseContent).HasColumnType("text");
                entity.Property(e => e.DurationMs);
                entity.Property(e => e.ErrorMessage).HasColumnType("text");
                entity.Property(e => e.CorrelationId);
                entity.Property(e => e.ClientIP).HasMaxLength(50);
                entity.Property(e => e.GemaaktOp).IsRequired().HasDefaultValueSql("NOW()");

                // Indexes
                entity.HasIndex(e => e.GemaaktOp).HasDatabaseName("IX_GemaaktOp");
                entity.HasIndex(e => e.ResponseStatus).HasDatabaseName("IX_ResponseStatus");
            });
        }
    }
}
