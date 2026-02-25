using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.DAL
{
    public class VectorDbContext : DbContext
    {
        public VectorDbContext(DbContextOptions<VectorDbContext> options)
            : base(options)
        {
        }

        public DbSet<Chunk> Chunks { get; set; }
        public DbSet<Bron> Bronnen { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ScrapingElement> ScrapingElement { get; set; }
        public DbSet<ScrapingUrlBlackList> ScrapingUrlBlackList { get; set; }
        public DbSet<PromptType> PromptTypes { get; set; }
        public DbSet<Prompt> Prompts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            // Project configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("project");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Naam).HasMaxLength(200);
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();
            });

            // Bron configuration
            modelBuilder.Entity<Bron>(entity =>
            {
                entity.ToTable("bron");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany(p => p.Bronnen)
                    .HasForeignKey(e => e.Project)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ScrapingElement>(entity =>
            {
                entity.ToTable("scrapingelement");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.ElementName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Selector).HasMaxLength(500).IsRequired();
                entity.Property(e => e.IsRequired).IsRequired();
                entity.Property(e => e.SortOrder).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany(p => p.ScrapingElements)
                    .HasForeignKey(e => e.Project)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ScrapingUrlBlackList>(entity =>
            {
                entity.ToTable("scrapingurlblacklist");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.BlackListElement).HasMaxLength(200).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany(p => p.ScrapingUrlBlackLists)
                    .HasForeignKey(e => e.Project)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Chunk configuration
            modelBuilder.Entity<Chunk>(entity =>
            {
                entity.ToTable("chunk");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.BronID).IsRequired();
                entity.Property(e => e.Tekst).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                // EF Core doesn't natively support VECTOR type yet in SQL Server 2025
                // We'll handle vector operations with raw SQL
                entity.Ignore(e => e.TekstVector);

                entity.HasOne(e => e.Bron)
                    .WithMany(b => b.Chunks)
                    .HasForeignKey(e => e.BronID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PromptType configuration
            modelBuilder.Entity<PromptType>(entity =>
            {
                entity.ToTable("prompttype");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Omschrijving).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp);
                entity.Property(e => e.Status).IsRequired();
            });

            // Prompt configuration
            modelBuilder.Entity<Prompt>(entity =>
            {
                entity.ToTable("prompt");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.PromptType).IsRequired();
                entity.Property(e => e.SystemInstruction).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.ResponseSchema).IsRequired();
                entity.Property(e => e.MaxTokens).IsRequired();
                entity.Property(e => e.Temperature).IsRequired();
                entity.Property(e => e.TopP).IsRequired();
                entity.Property(e => e.TopK).IsRequired();
                entity.Property(e => e.Model).IsRequired();
                entity.Property(e => e.Volgorde).IsRequired();

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany(p => p.Prompts)
                    .HasForeignKey(e => e.Project)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PromptTypeNavigation)
                    .WithMany(pt => pt.Prompts)
                    .HasForeignKey(e => e.PromptType)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}