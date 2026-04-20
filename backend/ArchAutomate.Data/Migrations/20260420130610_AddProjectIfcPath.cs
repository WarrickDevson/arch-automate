using System;
using ArchAutomate.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchAutomate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIfcPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ifc_path",
                table: "projects",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ifc_path",
                table: "projects");
        }
    }
}
