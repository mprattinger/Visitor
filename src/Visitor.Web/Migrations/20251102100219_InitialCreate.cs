using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Visitor.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Visitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PlannedDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArrivedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByEntraId = table.Column<string>(type: "TEXT", nullable: true),
                    VisitorToken = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_CreatedAt",
                table: "Visitors",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_Name_Company_CreatedAt",
                table: "Visitors",
                columns: new[] { "Name", "Company", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_Status",
                table: "Visitors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_VisitorToken",
                table: "Visitors",
                column: "VisitorToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Visitors");
        }
    }
}
