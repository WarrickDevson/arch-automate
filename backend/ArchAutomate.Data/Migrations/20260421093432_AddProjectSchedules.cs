using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchAutomate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: table created via Supabase SQL migration 20260421000010_create_project_schedules.sql.
            // This migration exists only to advance the EF model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: matching Up().
        }
    }
}
