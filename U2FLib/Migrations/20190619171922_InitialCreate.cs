using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace U2FLib.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationDatum",
                columns: table => new
                {
                    Id = table.Column<uint>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Counter = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDatum", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyPairs",
                columns: table => new
                {
                    KeyHandle = table.Column<string>(nullable: false),
                    Label = table.Column<string>(nullable: true),
                    ApplicationTag = table.Column<byte[]>(nullable: true),
                    PublicKey = table.Column<byte[]>(nullable: true),
                    PrivateKey = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyPairs", x => x.KeyHandle);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDatum");

            migrationBuilder.DropTable(
                name: "KeyPairs");
        }
    }
}
