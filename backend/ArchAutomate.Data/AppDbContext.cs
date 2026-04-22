using ArchAutomate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<RejectionComment> RejectionComments => Set<RejectionComment>();
    public DbSet<Stakeholder> Stakeholders => Set<Stakeholder>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<ProjectSchedule> ProjectSchedules => Set<ProjectSchedule>();
    public DbSet<ProjectTally> ProjectTallies => Set<ProjectTally>();
    public DbSet<ProjectSpec> ProjectSpecs => Set<ProjectSpec>();
    public DbSet<ProjectFoundationCheck> ProjectFoundationChecks => Set<ProjectFoundationCheck>();
    public DbSet<ProjectRoofCheck> ProjectRoofChecks => Set<ProjectRoofCheck>();
    public DbSet<ProjectGasCheck> ProjectGasChecks => Set<ProjectGasCheck>();

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
            e.Property(p => p.MunicipalityId).HasColumnName("municipality_id");
            e.Property(p => p.Erf).HasColumnName("erf");
            e.Property(p => p.Address).HasColumnName("address");
            e.Property(p => p.ProposedGfaM2).HasColumnName("proposed_gfa_m2");
            e.Property(p => p.FootprintM2).HasColumnName("footprint_m2");
            e.Property(p => p.NumberOfStoreys).HasColumnName("number_of_storeys");
            e.Property(p => p.BuildingHeightM).HasColumnName("building_height_m");
            e.Property(p => p.FrontSetbackM).HasColumnName("front_setback_m");
            e.Property(p => p.RearSetbackM).HasColumnName("rear_setback_m");
            e.Property(p => p.SideSetbackM).HasColumnName("side_setback_m");
            e.Property(p => p.ParkingBays).HasColumnName("parking_bays");
            e.Property(p => p.GlaForParkingM2).HasColumnName("gla_for_parking_m2");
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

        modelBuilder.Entity<Municipality>(e =>
        {
            e.ToTable("municipalities");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.ProvinceId).HasColumnName("province_id");
            e.Property(m => m.Name).HasColumnName("name");
            e.Property(m => m.ShortName).HasColumnName("short_name");
            e.Property(m => m.Category).HasColumnName("category");
            e.Property(m => m.ZoningScheme).HasColumnName("zoning_scheme");
            e.Property(m => m.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<ProjectSchedule>(e =>
        {
            e.ToTable("project_schedules");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.ProjectId).HasColumnName("project_id");
            e.Property(s => s.TenantId).HasColumnName("tenant_id");
            e.Property(s => s.ExtractedAt).HasColumnName("extracted_at");
            e.Property(s => s.DoorScheduleJson).HasColumnName("door_schedule").HasColumnType("jsonb");
            e.Property(s => s.WindowScheduleJson).HasColumnName("window_schedule").HasColumnType("jsonb");
            e.Property(s => s.DoorCount).HasColumnName("door_count");
            e.Property(s => s.WindowCount).HasColumnName("window_count");
            e.HasIndex(s => s.TenantId);
            e.HasIndex(s => s.ProjectId).IsUnique();
            e.HasOne(s => s.Project)
             .WithMany()
             .HasForeignKey(s => s.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectTally>(e =>
        {
            e.ToTable("project_tallies");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.ProjectId).HasColumnName("project_id");
            e.Property(t => t.TenantId).HasColumnName("tenant_id");
            e.Property(t => t.ExtractedAt).HasColumnName("extracted_at");
            e.Property(t => t.TallyJson).HasColumnName("tally").HasColumnType("jsonb");
            e.Property(t => t.LightingCount).HasColumnName("lighting_count");
            e.Property(t => t.ElectricalCount).HasColumnName("electrical_count");
            e.Property(t => t.SanitaryCount).HasColumnName("sanitary_count");
            e.Property(t => t.HvacCount).HasColumnName("hvac_count");
            e.Property(t => t.FireCount).HasColumnName("fire_count");
            e.Property(t => t.OtherCount).HasColumnName("other_count");
            e.Property(t => t.TotalCount).HasColumnName("total_count");
            e.HasIndex(t => t.TenantId);
            e.HasIndex(t => t.ProjectId).IsUnique();
            e.HasOne(t => t.Project)
             .WithMany()
             .HasForeignKey(t => t.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectSpec>(e =>
        {
            e.ToTable("project_specs");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.ProjectId).HasColumnName("project_id");
            e.Property(s => s.TenantId).HasColumnName("tenant_id");
            e.Property(s => s.ExtractedAt).HasColumnName("extracted_at");
            e.Property(s => s.CompiledAt).HasColumnName("compiled_at");
            e.Property(s => s.MaterialsJson).HasColumnName("materials").HasColumnType("jsonb");
            e.Property(s => s.SpecJson).HasColumnName("spec").HasColumnType("jsonb");
            e.Property(s => s.ClauseCount).HasColumnName("clause_count");
            e.HasIndex(s => s.TenantId);
            e.HasIndex(s => s.ProjectId).IsUnique();
            e.HasOne(s => s.Project)
             .WithMany()
             .HasForeignKey(s => s.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectFoundationCheck>(e =>
        {
            e.ToTable("project_foundation_checks");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.ProjectId).HasColumnName("project_id");
            e.Property(f => f.TenantId).HasColumnName("tenant_id");
            e.Property(f => f.CheckedAt).HasColumnName("checked_at");
            e.Property(f => f.InputJson).HasColumnName("input").HasColumnType("jsonb");
            e.Property(f => f.ResultsJson).HasColumnName("results").HasColumnType("jsonb");
            e.Property(f => f.OverallPass).HasColumnName("overall_pass");
            e.Property(f => f.NumberOfStoreys).HasColumnName("number_of_storeys");
            e.HasIndex(f => f.TenantId);
            e.HasIndex(f => f.ProjectId).IsUnique();
            e.HasOne(f => f.Project)
             .WithMany()
             .HasForeignKey(f => f.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectRoofCheck>(e =>
        {
            e.ToTable("project_roof_checks");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.ProjectId).HasColumnName("project_id");
            e.Property(r => r.TenantId).HasColumnName("tenant_id");
            e.Property(r => r.CheckedAt).HasColumnName("checked_at");
            e.Property(r => r.InputJson).HasColumnName("input").HasColumnType("jsonb");
            e.Property(r => r.ResultsJson).HasColumnName("results").HasColumnType("jsonb");
            e.Property(r => r.OverallPass).HasColumnName("overall_pass");
            e.Property(r => r.RoofType).HasColumnName("roof_type");
            e.HasIndex(r => r.TenantId);
            e.HasIndex(r => r.ProjectId).IsUnique();
            e.HasOne(r => r.Project)
             .WithMany()
             .HasForeignKey(r => r.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectGasCheck>(e =>
        {
            e.ToTable("project_gas_checks");
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasColumnName("id");
            e.Property(g => g.ProjectId).HasColumnName("project_id");
            e.Property(g => g.TenantId).HasColumnName("tenant_id");
            e.Property(g => g.CheckedAt).HasColumnName("checked_at");
            e.Property(g => g.InputJson).HasColumnName("input").HasColumnType("jsonb");
            e.Property(g => g.ResultsJson).HasColumnName("results").HasColumnType("jsonb");
            e.Property(g => g.OverallPass).HasColumnName("overall_pass");
            e.Property(g => g.HasGasInstallation).HasColumnName("has_gas_installation");
            e.HasIndex(g => g.TenantId);
            e.HasIndex(g => g.ProjectId).IsUnique();
            e.HasOne(g => g.Project)
             .WithMany()
             .HasForeignKey(g => g.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
