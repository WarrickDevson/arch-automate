using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchAutomate.Data.Migrations
{
    public partial class AddProjectAddress : Migration
    {
        // All columns already exist from Supabase SQL migrations (20260420000004_create_projects.sql).
        // This is a no-op to keep EF Core model snapshot in sync.
        protected override void Up(MigrationBuilder migrationBuilder) { }
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
