using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmeWatches.Api.Migrations
{
    /// <inheritdoc />
    public partial class FlexibleProductSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WaterResistance",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Numrat e vjetër (metra) kalojnë si tekst: 50 -> "50 m"; vlera 0 mbetet bosh.
            migrationBuilder.Sql("UPDATE Products SET WaterResistance = CONCAT(WaterResistanceM, N' m') WHERE WaterResistanceM > 0;");

            migrationBuilder.DropColumn(
                name: "WaterResistanceM",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "CaseSizeMm",
                table: "Products",
                type: "decimal(5,1)",
                precision: 5,
                scale: 1,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaterResistance",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "CaseSizeMm",
                table: "Products",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,1)",
                oldPrecision: 5,
                oldScale: 1,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaterResistanceM",
                table: "Products",
                type: "int",
                nullable: true);
        }
    }
}
