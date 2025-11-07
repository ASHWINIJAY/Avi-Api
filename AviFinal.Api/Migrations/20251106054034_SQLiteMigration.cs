using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AviFinal.Api.Migrations
{
    /// <inheritdoc />
    public partial class SQLiteMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocoDashboard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InspectorId = table.Column<string>(type: "TEXT", nullable: false),
                    InspectorName = table.Column<string>(type: "TEXT", nullable: false),
                    LocoNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    LocoClass = table.Column<string>(type: "TEXT", nullable: false),
                    LocoModel = table.Column<string>(type: "TEXT", nullable: false),
                    DateAssessed = table.Column<string>(type: "TEXT", nullable: false),
                    TimeAssessed = table.Column<string>(type: "TEXT", nullable: false),
                    LocoPhoto = table.Column<string>(type: "TEXT", nullable: true),
                    BodyDamage = table.Column<string>(type: "TEXT", nullable: false),
                    BodyPhotos = table.Column<string>(type: "TEXT", nullable: true),
                    RefurbishValue = table.Column<string>(type: "TEXT", nullable: false),
                    MissingValue = table.Column<string>(type: "TEXT", nullable: false),
                    ReplaceValue = table.Column<string>(type: "TEXT", nullable: false),
                    MissingPhotos = table.Column<string>(type: "TEXT", nullable: true),
                    ReplacePhotos = table.Column<string>(type: "TEXT", nullable: true),
                    AssessmentQuote = table.Column<string>(type: "TEXT", nullable: true),
                    AssessmentCert = table.Column<string>(type: "TEXT", nullable: true),
                    UploadStatus = table.Column<string>(type: "TEXT", nullable: false),
                    UploadDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocoDashboard", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocoDashboard");
        }
    }
}
