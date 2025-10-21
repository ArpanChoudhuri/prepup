using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Processed_NoIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create new table without IDENTITY
            migrationBuilder.CreateTable(
                name: "Processed_new",
                columns: table => new
                {
                    MessageId = table.Column<long>(nullable: false),
                    ProcessedUtc = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processed_new", x => x.MessageId);
                });

            // 2) Copy data from old table if it exists
            migrationBuilder.Sql(@"
        IF OBJECT_ID('dbo.Processed', 'U') IS NOT NULL
        BEGIN
            SET IDENTITY_INSERT dbo.Processed ON; -- in case old was identity
            INSERT INTO dbo.Processed_new (MessageId, ProcessedUtc)
            SELECT MessageId, ProcessedUtc FROM dbo.Processed;
            SET IDENTITY_INSERT dbo.Processed OFF;
        END
    ");

            // 3) Drop old and rename
            migrationBuilder.Sql(@"
        IF OBJECT_ID('dbo.Processed', 'U') IS NOT NULL
            DROP TABLE dbo.Processed;
    ");
            migrationBuilder.Sql(@"EXEC sp_rename 'dbo.Processed_new', 'Processed';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate as identity if you ever rollback (previous broken shape)
            migrationBuilder.CreateTable(
                name: "Processed",
                columns: table => new
                {
                    MessageId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessedUtc = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processed", x => x.MessageId);
                });
        }

    }
}
