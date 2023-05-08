using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthenticationMicroservice.Migrations
{
    /// <inheritdoc />
    public partial class RemovedProfilePictures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
