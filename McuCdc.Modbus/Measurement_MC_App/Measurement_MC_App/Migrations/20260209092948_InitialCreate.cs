using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Measurement_MC_App.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Connectors",
                columns: table => new
                {
                    ConnectorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Host = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PortName = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaudRate = table.Column<int>(type: "int", nullable: false),
                    DataBits = table.Column<int>(type: "int", nullable: false),
                    Parity = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StopBits = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpeedX = table.Column<int>(type: "int", nullable: false),
                    SpeedY = table.Column<int>(type: "int", nullable: false),
                    SpeedZ = table.Column<int>(type: "int", nullable: false),
                    AxisXMax = table.Column<int>(type: "int", nullable: false),
                    AxisYMax = table.Column<int>(type: "int", nullable: false),
                    AxisZMax = table.Column<int>(type: "int", nullable: false),
                    SafeZ = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connectors", x => x.ConnectorId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    ModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NominalX = table.Column<double>(type: "double", nullable: false),
                    NominalY = table.Column<double>(type: "double", nullable: false),
                    TolXPlus = table.Column<double>(type: "double", nullable: false),
                    TolXMinus = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.ModelId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    trayId = table.Column<int>(type: "int", nullable: false),
                    row = table.Column<int>(type: "int", nullable: false),
                    column = table.Column<int>(type: "int", nullable: false),
                    width = table.Column<double>(type: "double", nullable: false),
                    height = table.Column<double>(type: "double", nullable: false),
                    status = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.ModelId);
                    table.ForeignKey(
                        name: "FK_Logs_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PointParams",
                columns: table => new
                {
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    point1X = table.Column<int>(type: "int", nullable: false),
                    point1Y = table.Column<int>(type: "int", nullable: false),
                    point1Z = table.Column<int>(type: "int", nullable: false),
                    point2X = table.Column<int>(type: "int", nullable: false),
                    point2Y = table.Column<int>(type: "int", nullable: false),
                    point2Z = table.Column<int>(type: "int", nullable: false),
                    point3X = table.Column<int>(type: "int", nullable: false),
                    point3Y = table.Column<int>(type: "int", nullable: false),
                    point3Z = table.Column<int>(type: "int", nullable: false),
                    point4X = table.Column<int>(type: "int", nullable: false),
                    point4Y = table.Column<int>(type: "int", nullable: false),
                    point4Z = table.Column<int>(type: "int", nullable: false),
                    point5X = table.Column<int>(type: "int", nullable: false),
                    point5Y = table.Column<int>(type: "int", nullable: false),
                    point5Z = table.Column<int>(type: "int", nullable: false),
                    point6X = table.Column<int>(type: "int", nullable: false),
                    point6Y = table.Column<int>(type: "int", nullable: false),
                    point6Z = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointParams", x => x.ModelId);
                    table.ForeignKey(
                        name: "FK_PointParams_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VisionParams",
                columns: table => new
                {
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    Blur = table.Column<double>(type: "double", nullable: false),
                    CannyLow = table.Column<double>(type: "double", nullable: false),
                    CannyHigh = table.Column<double>(type: "double", nullable: false),
                    RefObjectWidth = table.Column<double>(type: "double", nullable: false),
                    RefObjectHeight = table.Column<double>(type: "double", nullable: false),
                    MmPerPixelWidth = table.Column<double>(type: "double", nullable: false),
                    MmPerPixelHeight = table.Column<double>(type: "double", nullable: false),
                    RealObjectWidth = table.Column<double>(type: "double", nullable: false),
                    RealObjectHeight = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionParams", x => x.ModelId);
                    table.ForeignKey(
                        name: "FK_VisionParams_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connectors");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "PointParams");

            migrationBuilder.DropTable(
                name: "VisionParams");

            migrationBuilder.DropTable(
                name: "Models");
        }
    }
}
