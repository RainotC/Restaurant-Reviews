using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurants.Migrations
{
    /// <inheritdoc />
    public partial class AddAdressAsOwnedType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Adress",
                table: "Restaurants",
                newName: "Address_ZipCode");

            migrationBuilder.AddColumn<string>(
                name: "Address_City",
                table: "Restaurants",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Street",
                table: "Restaurants",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_City",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "Address_Street",
                table: "Restaurants");

            migrationBuilder.RenameColumn(
                name: "Address_ZipCode",
                table: "Restaurants",
                newName: "Adress");
        }
    }
}
