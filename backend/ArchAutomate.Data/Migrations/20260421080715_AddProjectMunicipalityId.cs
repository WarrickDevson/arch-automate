using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchAutomate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectMunicipalityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: all columns already exist in Supabase via SQL migrations.
            // This migration exists only to advance the EF model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: matching Up().
        }
    }
}
