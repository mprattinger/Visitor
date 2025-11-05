using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Visitor.Web.Migrations
{
    /// <inheritdoc />
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    public partial class optimizingvisitor : Migration
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
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
