using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KikiAZZoo.Migrations
{
    /// <inheritdoc />
    public partial class MakeMaxUsageNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Coupons') 
                      AND name = 'MaxUsageCount'
                )
                BEGIN
                    EXEC sp_rename 'Coupons.MaxUsageCount', 'MaxUsage', 'COLUMN';
                END
            ");

            migrationBuilder.AlterColumn<int>(
                name: "MaxUsage",
                table: "Coupons",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaxUsage",
                table: "Coupons",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "MaxUsage",
                table: "Coupons",
                newName: "MaxUsageCount");
        }
    }
}
