using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Processed_NoIdentity_Fix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Ensure any leftover constraint or temp table from a previous failed run is removed
IF EXISTS(SELECT 1 FROM sys.objects WHERE name = 'PK_Processed_new' AND type = 'PK')
BEGIN
    DECLARE @parent SYSNAME;
    SELECT TOP(1) @parent = OBJECT_NAME(parent_object_id) FROM sys.objects WHERE name = 'PK_Processed_new' AND type = 'PK';
    IF @parent IS NOT NULL
    BEGIN
        DECLARE @dropSql nvarchar(max) = N'ALTER TABLE dbo.' + QUOTENAME(@parent) + N' DROP CONSTRAINT PK_Processed_new;';
        EXEC sp_executesql @dropSql;
    END
END

IF OBJECT_ID('dbo.Processed_new', 'U') IS NOT NULL
    DROP TABLE dbo.Processed_new;

IF OBJECT_ID('dbo.Processed', 'U') IS NOT NULL
BEGIN
    CREATE TABLE dbo.Processed_new
    (
        MessageId bigint NOT NULL,
        ProcessedUtc datetime2 NOT NULL
    );

    -- Copy data from the existing table into the new table (no IDENTITY_INSERT required
    -- because the target column is NOT an IDENTITY column).
    INSERT INTO dbo.Processed_new (MessageId, ProcessedUtc)
    SELECT MessageId, ProcessedUtc FROM dbo.Processed;

    -- Create PK after data copy
    ALTER TABLE dbo.Processed_new ADD CONSTRAINT PK_Processed_new PRIMARY KEY (MessageId);

    DROP TABLE dbo.Processed;
    EXEC sp_rename 'dbo.Processed_new', 'Processed';
END
ELSE
BEGIN
    -- If the table does not exist yet, create it without IDENTITY
    CREATE TABLE dbo.Processed
    (
        MessageId bigint NOT NULL PRIMARY KEY,
        ProcessedUtc datetime2 NOT NULL
    );
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate previous shape (with IDENTITY) if rolling back
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Processed', 'U') IS NOT NULL
    DROP TABLE dbo.Processed;

CREATE TABLE dbo.Processed
(
    MessageId bigint NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ProcessedUtc datetime2 NOT NULL
);
");
        }
    }
}
