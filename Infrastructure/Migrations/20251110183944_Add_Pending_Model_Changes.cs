using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Add_Pending_Model_Changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: changing a primary key column type (int identity -> Guid) cannot be done with ALTER.
            // This migration recreates the affected tables with the desired schema.
            // WARNING: this approach will remove existing data in Products and Orders (dev/test only).
            // If you need to preserve data, implement a copy-and-map strategy instead.

            // Drop dependent tables first (if they exist)
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItem','U') IS NOT NULL
    DROP TABLE dbo.OrderItem;
");

            // Drop Orders (if exists)
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Orders','U') IS NOT NULL
    DROP TABLE dbo.Orders;
");

            // Drop Products (if exists)
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Products','U') IS NOT NULL
    DROP TABLE dbo.Products;
");

            // Recreate Products with Guid primary key and new columns
            migrationBuilder.Sql(@"
CREATE TABLE dbo.Products
(
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Name nvarchar(120) NOT NULL,
    Price decimal(10,2) NOT NULL,
    RowVersion rowversion NOT NULL,
    IsActive bit NOT NULL DEFAULT(0),
    UnitPrice int NOT NULL DEFAULT(0)
);

CREATE INDEX IX_Products_Name ON dbo.Products (Name);
");

            // Recreate Orders with Guid primary key and new columns
            migrationBuilder.Sql(@"
CREATE TABLE dbo.Orders
(
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    CustomerId nvarchar(max) NOT NULL,
    OrderDate datetime2 NOT NULL,
    Status nvarchar(max) NOT NULL,
    Total decimal(18,2) NOT NULL,
    Tenant nvarchar(max) NOT NULL,
    ProductId uniqueidentifier NOT NULL,
    CreatedUtc datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    RowVersion rowversion NOT NULL
);
");

            // Create OrderItem table
            migrationBuilder.Sql(@"
CREATE TABLE dbo.OrderItem
(
    OrderId uniqueidentifier NOT NULL,
    ProductId uniqueidentifier NOT NULL,
    Quantity int NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    LineTotal decimal(18,2) NOT NULL,
    CONSTRAINT PK_OrderItem PRIMARY KEY (OrderId, ProductId),
    CONSTRAINT FK_OrderItem_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore previous (int identity) shapes if rolling back.
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItem','U') IS NOT NULL
    DROP TABLE dbo.OrderItem;
IF OBJECT_ID('dbo.Orders','U') IS NOT NULL
    DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products','U') IS NOT NULL
    DROP TABLE dbo.Products;

CREATE TABLE dbo.Products
(
    Id int NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(120) NOT NULL,
    Price decimal(18,2) NOT NULL,
    RowVersion rowversion NOT NULL
);

CREATE TABLE dbo.Orders
(
    Id int NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ProductId int NOT NULL,
    Total decimal(18,2) NOT NULL
);

CREATE TABLE dbo.OrderItem
(
    OrderId int NOT NULL,
    ProductId int NOT NULL,
    Quantity int NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    LineTotal decimal(18,2) NOT NULL,
    CONSTRAINT PK_OrderItem PRIMARY KEY (OrderId, ProductId),
    CONSTRAINT FK_OrderItem_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE
);
");
        }
    }
}
