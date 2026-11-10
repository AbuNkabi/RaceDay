using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.Api.Data;

#nullable disable
namespace RaceDay.Api.Migrations;

[DbContext(typeof(RaceDayDbContext))]
[Migration("20261110190000_AddPart3Features")]
public partial class AddPart3Features : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "EntryFee", table: "Categories",
            type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<int>(name: "MaximumParticipants", table: "Categories",
            type: "int", nullable: false, defaultValue: 100);
        migrationBuilder.AddColumn<int>(name: "CategoryId", table: "Enrolments",
            type: "int", nullable: true);
        // Legacy enrolments retain their working status while new entries start as Pending.
        migrationBuilder.AddColumn<string>(name: "Status", table: "Enrolments",
            type: "nvarchar(25)", maxLength: 25, nullable: false, defaultValue: "Confirmed");
        migrationBuilder.AddColumn<string>(name: "ImageUrl", table: "Events",
            type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProfileImageUrl", table: "Users",
            type: "nvarchar(max)", nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EntryFee", table: "Categories");
        migrationBuilder.DropColumn(name: "MaximumParticipants", table: "Categories");
        migrationBuilder.DropColumn(name: "CategoryId", table: "Enrolments");
        migrationBuilder.DropColumn(name: "Status", table: "Enrolments");
        migrationBuilder.DropColumn(name: "ImageUrl", table: "Events");
        migrationBuilder.DropColumn(name: "ProfileImageUrl", table: "Users");
    }
}
