using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercorClone.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "JobPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "JobPosts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "JobPosts",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "JobPosts");
        }
    }
}
