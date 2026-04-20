using ArchAutomate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<RejectionComment> RejectionComments => Set<RejectionComment>();
    public DbSet<Stakeholder> Stakeholders => Set<Stakeholder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Register PostgreSQL enum types (must match public.* enums in Supabase SQL migrations)
        modelBuilder.HasPostgresEnum<ProjectStatus>("public", "project_status");
        modelBuilder.HasPostgresEnum<StakeholderRole>("public", "stakeholder_role");
        modelBuilder.HasPostgresEnum<RejectionCategory>("public", "rejection_category");
        modelBuilder.HasPostgresEnum<RejectionStatus>("public", "rejection_status");

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            e.Property(p => p.Description).HasColumnName("description");
            e.Property(p => p.TenantId).HasColumnName("tenant_id");
            e.Property(p => p.OwnerUserId).HasColumnName("owner_user_id");
            e.Property(p => p.SiteAreaM2).HasColumnName("site_area_m2");
            e.Property(p => p.ZoningScheme).HasColumnName("zoning_scheme");
            e.Property(p => p.Municipality).HasColumnName("municipality_name");
            e.Property(p => p.Erf).HasColumnName("erf");
            e.Property(p => p.Status).HasColumnName("status");
            e.Property(p => p.IfcPath).HasColumnName("ifc_path");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(p => p.TenantId);
            e.HasIndex(p => p.OwnerUserId);
        });

        modelBuilder.Entity<RejectionComment>(e =>
        {
            e.ToTable("rejection_comments");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.ProjectId).HasColumnName("project_id");
            e.Property(r => r.SourceDocument).HasColumnName("source_document");
            e.Property(r => r.ClauseReference).HasColumnName("clause_reference");
            e.Property(r => r.CommentText).HasColumnName("comment_text");
            e.Property(r => r.ParsedAction).HasColumnName("parsed_action");
            e.Property(r => r.Category).HasColumnName("category");
            e.Property(r => r.Status).HasColumnName("status");
            e.Property(r => r.ReceivedAt).HasColumnName("received_at");
            e.Property(r => r.ResolvedAt).HasColumnName("resolved_at");
            e.HasOne(r => r.Project)
             .WithMany(p => p.RejectionComments)
             .HasForeignKey(r => r.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Stakeholder>(e =>
        {
            e.ToTable("stakeholders");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.ProjectId).HasColumnName("project_id");
            e.Property(s => s.Name).HasColumnName("name");
            e.Property(s => s.Organisation).HasColumnName("organisation");
            e.Property(s => s.Email).HasColumnName("email");
            e.Property(s => s.Phone).HasColumnName("phone");
            e.Property(s => s.Role).HasColumnName("role");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.HasOne(s => s.Project)
             .WithMany(p => p.Stakeholders)
             .HasForeignKey(s => s.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
