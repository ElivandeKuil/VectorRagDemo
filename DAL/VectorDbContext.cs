using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using VectorRagDemo.Models.Entities;
using Conversation = VectorRagDemo.Models.Entities.Management.Conversation;
using Message = VectorRagDemo.Models.Entities.Management.Message;

namespace VectorRagDemo.DAL
{
    public class VectorDbContext : DbContext
    {
        public VectorDbContext(DbContextOptions<VectorDbContext> options)
            : base(options)
        {
        }

        public DbSet<WidgetConfig> WidgetConfigs { get; set; }
        public DbSet<Chunk> Chunks { get; set; }
        public DbSet<Bron> Bronnen { get; set; }
        public DbSet<Project> Projects { get; set; }

        public DbSet<PromptType> PromptTypes { get; set; }
        public DbSet<Prompt> Prompts { get; set; }
        public DbSet<GebruikerProject> GebruikerProjecten { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            // WidgetConfig configuration
            modelBuilder.Entity<WidgetConfig>(entity =>
            {
                entity.ToTable("widgetconfig");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ProjectID).IsRequired();
                entity.Property(e => e.WidgetPosition).HasMaxLength(20).IsRequired();
                entity.Property(e => e.ButtonColor).HasMaxLength(20).IsRequired();
                entity.Property(e => e.HeaderBgColor).HasMaxLength(20).IsRequired();
                entity.Property(e => e.HeaderTextColor).HasMaxLength(20).IsRequired();
                entity.Property(e => e.UserBubbleBgColor).HasMaxLength(20).IsRequired();
                entity.Property(e => e.UserBubbleTextColor).HasMaxLength(20).IsRequired();
                entity.Property(e => e.BotBubbleBgColor).HasMaxLength(20).IsRequired();
                entity.Property(e => e.BotBubbleTextColor).HasMaxLength(20).IsRequired();
                entity.Property(e => e.WidgetFontSize).HasMaxLength(5).IsRequired();
                entity.Property(e => e.GreetingMessage).HasMaxLength(500).IsRequired();

                entity.HasOne(e => e.Project)
                    .WithOne(p => p.WidgetConfig)
                    .HasForeignKey<WidgetConfig>(e => e.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Project configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("project");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Naam).HasMaxLength(200);
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.EmbedKey).HasMaxLength(50);
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

                entity.HasOne(e => e.FolderNavigation)
                    .WithMany()
                    .HasForeignKey(e => e.FolderId)
                    .OnDelete(DeleteBehavior.SetNull);
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

            // Folder configuration
            modelBuilder.Entity<Folder>(entity =>
            {
                entity.ToTable("folder");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Naam).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany()
                    .HasForeignKey(e => e.Project)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // GebruikerProject configuration
            modelBuilder.Entity<GebruikerProject>(entity =>
            {
                entity.ToTable("gebruikerproject");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Gebruiker).IsRequired();
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany()
                    .HasForeignKey(e => e.Project)
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
            // Conversation configuration
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("conversation");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Gebruiker);                   // nullable: null = widget
                entity.Property(e => e.SessionToken).HasMaxLength(100); // null = studio
                entity.Property(e => e.BronType).HasMaxLength(10).IsRequired().HasDefaultValue("studio");
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasIndex(e => e.SessionToken)
                    .HasDatabaseName("IX_Conversation_SessionToken")
                    .IsUnique()
                    .HasFilter("sessiontoken IS NOT NULL");
            });

            // Message configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("message");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Conversation).IsRequired();
                entity.Property(e => e.Tekst).HasMaxLength(10000).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.SenderType).IsRequired();
            });
        }
    }
}