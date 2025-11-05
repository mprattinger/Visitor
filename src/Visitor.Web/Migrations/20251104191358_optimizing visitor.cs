using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Visitor.Web.Migrations
{
    /// <inheritdoc />
    public partial class optimizingvisitor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visitors_VisitorToken",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "PlannedDuration",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "VisitorToken",
                table: "Visitors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Visitors",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PlannedDuration",
                table: "Visitors",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "VisitorToken",
                table: "Visitors",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_VisitorToken",
                table: "Visitors",
                column: "VisitorToken",
                unique: true);
        }
    }
}
