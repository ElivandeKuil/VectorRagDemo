using Microsoft.EntityFrameworkCore;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.Entities.Management;
using MgmtGebruikerProject = VectorRagDemo.Models.Entities.Management.GebruikerProject;

namespace VectorRagDemo.DAL
{
    public class ManagementDbContext : DbContext
    {
        public ManagementDbContext(DbContextOptions<ManagementDbContext> options)
            : base(options)
        {
        }

        // User & role tables
        public DbSet<Gebruiker> Gebruikers { get; set; } = null!;
        public DbSet<Rol> Rollen { get; set; } = null!;
        public DbSet<GebruikerRol> GebruikerRollen { get; set; } = null!;

        // Project access tables
        public DbSet<MgmtProject> MgmtProjects { get; set; } = null!;
        public DbSet<MgmtSubProject> MgmtSubProjects { get; set; } = null!;
        public DbSet<MgmtGebruikerProject> GebruikerProjecten { get; set; } = null!;
        public DbSet<GebruikerSubProject> GebruikerSubProjecten { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Gebruiker configuration
            modelBuilder.Entity<Gebruiker>(entity =>
            {
                entity.ToTable("gebruiker");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Naam).HasMaxLength(200).IsRequired();
                entity.Property(e => e.GebruikersNaam).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Wachtwoord).HasMaxLength(500).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.WachtwoordWijzigen).IsRequired();
            });

            // Rol configuration
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("rol");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Omschrijving).HasMaxLength(200).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();
            });

            // GebruikerRol configuration
            modelBuilder.Entity<GebruikerRol>(entity =>
            {
                entity.ToTable("gebruikerrol");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Gebruiker).IsRequired();
                entity.Property(e => e.Rol).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.GebruikerNavigation)
                    .WithMany(g => g.GebruikerRollen)
                    .HasForeignKey(e => e.Gebruiker)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RolNavigation)
                    .WithMany(r => r.GebruikerRollen)
                    .HasForeignKey(e => e.Rol)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // MgmtProject configuration (maps to "project" table in Management DB)
            modelBuilder.Entity<MgmtProject>(entity =>
            {
                entity.ToTable("project");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Omschrijving).HasMaxLength(256).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();
            });

            // MgmtSubProject configuration (maps to "subproject" table)
            modelBuilder.Entity<MgmtSubProject>(entity =>
            {
                entity.ToTable("subproject");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Omschrijving).HasMaxLength(256).IsRequired();
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany(p => p.SubProjects)
                    .HasForeignKey(e => e.Project)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // MgmtGebruikerProject configuration
            modelBuilder.Entity<MgmtGebruikerProject>(entity =>
            {
                entity.ToTable("gebruikerproject");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Gebruiker).IsRequired();
                entity.Property(e => e.Project).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.GebruikerNavigation)
                    .WithMany()
                    .HasForeignKey(e => e.Gebruiker)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ProjectNavigation)
                    .WithMany(p => p.GebruikerProjecten)
                    .HasForeignKey(e => e.Project)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // GebruikerSubProject configuration
            modelBuilder.Entity<GebruikerSubProject>(entity =>
            {
                entity.ToTable("gebruikersubproject");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Gebruiker).IsRequired();
                entity.Property(e => e.SubProject).IsRequired();
                entity.Property(e => e.GemaaktOp).IsRequired();
                entity.Property(e => e.GewijzigdOp).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.GebruikerNavigation)
                    .WithMany()
                    .HasForeignKey(e => e.Gebruiker)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SubProjectNavigation)
                    .WithMany(sp => sp.GebruikerSubProjecten)
                    .HasForeignKey(e => e.SubProject)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
