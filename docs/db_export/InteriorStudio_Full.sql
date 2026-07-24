/*
  InteriorStudio — FULL database script (schema + seed + patches)
  Run once in SSMS / Azure Data Studio / sqlcmd against SQL Server / LocalDB.
  Replaces the previous multi-file setup (create + seed_* + patch_* + fix_*).
  Generated for PRN232 submission.
*/
SET NOCOUNT ON;
GO


/* ============================================================
   01 SCHEMA
   Source: create_InteriorStudio.sql
   ============================================================ */
GO
/*
  InteriorStudio — MSSQL create script
  Run once in SSMS / Azure Data Studio.
  Sync: docs/MSSQL_SCHEMA.md + docs/BE_FULL_SPEC.md
*/
SET NOCOUNT ON;
GO

IF DB_ID(N'InteriorStudio') IS NULL
BEGIN
  CREATE DATABASE InteriorStudio;
END
GO

USE InteriorStudio;
GO

/* ===== IDENTITY ===== */
CREATE TABLE Roles (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  Name        NVARCHAR(50)  NOT NULL UNIQUE, -- Customer, Sales, Manager, Admin
  Description NVARCHAR(255) NULL,
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Users (
  Id           INT IDENTITY(1,1) PRIMARY KEY,
  RoleId       INT           NOT NULL REFERENCES Roles(Id),
  Email        NVARCHAR(256) NOT NULL UNIQUE,
  PasswordHash NVARCHAR(512) NOT NULL,
  FullName     NVARCHAR(150) NOT NULL,
  Phone        NVARCHAR(30)  NULL,
  AvatarUrl    NVARCHAR(500) NULL,
  Address      NVARCHAR(500) NULL, -- profile
  IsLocked     BIT           NOT NULL DEFAULT 0,
  IsActive     BIT           NOT NULL DEFAULT 1,
  IsDeleted    BIT           NOT NULL DEFAULT 0,
  CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt    DATETIME2     NULL
);

CREATE TABLE RefreshTokens (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  UserId    INT           NOT NULL REFERENCES Users(Id),
  Token     NVARCHAR(512) NOT NULL,
  ExpiresAt DATETIME2     NOT NULL,
  CreatedAt DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  RevokedAt DATETIME2     NULL
);

CREATE TABLE PasswordResetTokens (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  UserId    INT           NOT NULL REFERENCES Users(Id),
  Token     NVARCHAR(128) NOT NULL,
  ExpiresAt DATETIME2     NOT NULL,
  UsedAt    DATETIME2     NULL
);

CREATE TABLE EmailOtps (
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  Email      NVARCHAR(256) NOT NULL,
  Purpose    NVARCHAR(30)  NOT NULL, -- Register | ResetPassword
  Otp        NVARCHAR(10)  NOT NULL,
  Payload    NVARCHAR(MAX) NULL,     -- JSON pending register
  ResetToken NVARCHAR(128) NULL,
  ExpiresAt  DATETIME2     NOT NULL,
  UsedAt     DATETIME2     NULL,
  CreatedAt  DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

/* ===== CATALOG ===== */
CREATE TABLE Categories (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  Name        NVARCHAR(120) NOT NULL,
  Description NVARCHAR(500) NULL,
  IsActive    BIT           NOT NULL DEFAULT 1,
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Products (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  CategoryId  INT            NULL REFERENCES Categories(Id),
  Name        NVARCHAR(200)  NOT NULL,
  Description NVARCHAR(MAX)  NULL,
  Price       DECIMAL(18,2)  NOT NULL CHECK (Price >= 0),
  MarketPrice DECIMAL(18,2)  NULL CHECK (MarketPrice IS NULL OR MarketPrice >= 0),
  Stock       INT            NOT NULL DEFAULT 0 CHECK (Stock >= 0),
  ImageUrl    NVARCHAR(500)  NULL,
  IsActive    BIT            NOT NULL DEFAULT 1,
  IsDeleted   BIT            NOT NULL DEFAULT 0,
  CreatedAt   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt   DATETIME2      NULL
);

CREATE TABLE ProductSpecs (
  ProductId       INT            NOT NULL PRIMARY KEY REFERENCES Products(Id) ON DELETE CASCADE,
  Dimensions      NVARCHAR(120)  NULL,
  Material        NVARCHAR(200)  NULL,
  Origin          NVARCHAR(100)  NULL,
  Finish          NVARCHAR(120)  NULL,
  WeightKg        DECIMAL(10,2)  NULL,
  WarrantyMonths  INT            NULL
);

CREATE TABLE ProductImages (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  ProductId INT           NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
  Url       NVARCHAR(500) NOT NULL,
  SortOrder INT           NOT NULL DEFAULT 0
);

/* ===== CART / ORDER ===== */
CREATE TABLE Carts (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  UserId    INT       NOT NULL UNIQUE REFERENCES Users(Id),
  UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE CartItems (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  CartId    INT NOT NULL REFERENCES Carts(Id) ON DELETE CASCADE,
  ProductId INT NOT NULL REFERENCES Products(Id),
  Quantity  INT NOT NULL CHECK (Quantity > 0),
  CONSTRAINT UQ_Cart_Product UNIQUE (CartId, ProductId)
);

CREATE TABLE Orders (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId      INT            NOT NULL REFERENCES Users(Id),
  Status          NVARCHAR(30)   NOT NULL, -- Pending/Processing/Shipping/Completed/Cancelled
  TotalPrice      DECIMAL(18,2)  NOT NULL,
  ShippingAddress NVARCHAR(500)  NOT NULL,
  Phone           NVARCHAR(30)   NOT NULL,
  CustomerName    NVARCHAR(150)  NULL, -- snapshot checkout
  CustomerEmail   NVARCHAR(256)  NULL, -- snapshot checkout
  Note            NVARCHAR(500)  NULL,
  CreatedAt       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2      NULL
);

CREATE TABLE OrderItems (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  OrderId     INT            NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
  ProductId   INT            NOT NULL REFERENCES Products(Id),
  ProductName NVARCHAR(200)  NOT NULL, -- snapshot
  UnitPrice   DECIMAL(18,2)  NOT NULL,
  Quantity    INT            NOT NULL CHECK (Quantity > 0)
);

CREATE TABLE ProductReviews (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  ProductId INT            NOT NULL REFERENCES Products(Id),
  UserId    INT            NOT NULL REFERENCES Users(Id),
  Rating    TINYINT        NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment   NVARCHAR(1000) NULL,
  CreatedAt DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT UQ_Review UNIQUE (ProductId, UserId)
);

/* ===== QUOTATION / DESIGN REQUEST ===== */
CREATE TABLE QuotationRequests (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId  INT           NOT NULL REFERENCES Users(Id),
  Title       NVARCHAR(200) NOT NULL,
  Description NVARCHAR(MAX) NULL,
  Status      NVARCHAR(30)  NOT NULL DEFAULT N'Pending', -- Pending/Replied
  Reply       NVARCHAR(MAX) NULL,
  HandledById INT           NULL REFERENCES Users(Id),
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt   DATETIME2     NULL
);

CREATE TABLE QuotationRequestProducts (
  QuotationRequestId INT NOT NULL REFERENCES QuotationRequests(Id) ON DELETE CASCADE,
  ProductId          INT NOT NULL REFERENCES Products(Id),
  PRIMARY KEY (QuotationRequestId, ProductId)
);

CREATE TABLE Quotations (
  Id                 INT IDENTITY(1,1) PRIMARY KEY,
  QuotationRequestId INT            NULL REFERENCES QuotationRequests(Id),
  CustomerId         INT            NOT NULL REFERENCES Users(Id),
  Title              NVARCHAR(200)  NULL,
  Amount             DECIMAL(18,2)  NOT NULL,
  Status             NVARCHAR(30)   NOT NULL, -- PendingApproval/Approved/Rejected
  Notes              NVARCHAR(1000) NULL,
  CreatedById        INT            NULL REFERENCES Users(Id),
  ApprovedById       INT            NULL REFERENCES Users(Id),
  CreatedAt          DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt          DATETIME2      NULL
);

CREATE TABLE QuotationProducts (
  QuotationId INT            NOT NULL REFERENCES Quotations(Id) ON DELETE CASCADE,
  ProductId   INT            NOT NULL REFERENCES Products(Id),
  Quantity    INT            NOT NULL DEFAULT 1 CHECK (Quantity > 0),
  UnitPrice   DECIMAL(18,2)  NULL,
  PRIMARY KEY (QuotationId, ProductId)
);

CREATE TABLE InteriorDesigns (
  Id             INT IDENTITY(1,1) PRIMARY KEY,
  Title          NVARCHAR(200)  NOT NULL,
  Category       NVARCHAR(50)   NULL, -- Living/Bedroom/Workspace/Kitchen
  Style          NVARCHAR(100)  NULL,
  ImageUrl       NVARCHAR(500)  NULL,
  Description    NVARCHAR(MAX)  NULL,
  AreaSqm        DECIMAL(10,2)  NULL,
  BudgetFrom     DECIMAL(18,2)  NULL,
  BudgetTo       DECIMAL(18,2)  NULL,
  TimelineWeeks  INT            NULL,
  StudioPrice    DECIMAL(18,2)  NULL,
  MarketAvgPrice DECIMAL(18,2)  NULL,
  IsPublished    BIT            NOT NULL DEFAULT 1,
  CreatedAt      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt      DATETIME2      NULL
);

CREATE TABLE InteriorDesignImages (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT           NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Url              NVARCHAR(500) NOT NULL,
  SortOrder        INT           NOT NULL DEFAULT 0
);

CREATE TABLE InteriorDesignHighlights (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Text             NVARCHAR(500)  NOT NULL,
  SortOrder        INT            NOT NULL DEFAULT 0
);

CREATE TABLE InteriorDesignSpecs (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Label            NVARCHAR(120)  NOT NULL,
  Value            NVARCHAR(300)  NOT NULL,
  SortOrder        INT            NOT NULL DEFAULT 0
);

CREATE TABLE InteriorDesignMaterials (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Name             NVARCHAR(150)  NOT NULL,
  Origin           NVARCHAR(100)  NULL,
  Finish           NVARCHAR(150)  NULL,
  Care             NVARCHAR(300)  NULL
);

CREATE TABLE InteriorDesignPackages (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Name             NVARCHAR(150)  NOT NULL,
  Price            DECIMAL(18,2)  NOT NULL,
  Includes         NVARCHAR(500)  NULL
);

CREATE TABLE InteriorDesignProducts (
  InteriorDesignId INT NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  ProductId        INT NOT NULL REFERENCES Products(Id),
  PRIMARY KEY (InteriorDesignId, ProductId)
);

CREATE TABLE DesignRequests (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId       INT            NOT NULL REFERENCES Users(Id),
  InteriorDesignId INT            NULL REFERENCES InteriorDesigns(Id),
  Title            NVARCHAR(200)  NOT NULL,
  Style            NVARCHAR(100)  NULL,
  Budget           DECIMAL(18,2)  NULL,
  Status           NVARCHAR(30)   NOT NULL DEFAULT N'New', -- New/InReview/Quoted/Done
  Notes            NVARCHAR(MAX)  NULL,
  AssignedToId     INT            NULL REFERENCES Users(Id),
  CreatedAt        DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt        DATETIME2      NULL
);

CREATE TABLE DesignRequestProducts (
  DesignRequestId INT NOT NULL REFERENCES DesignRequests(Id) ON DELETE CASCADE,
  ProductId       INT NOT NULL REFERENCES Products(Id),
  PRIMARY KEY (DesignRequestId, ProductId)
);

CREATE TABLE DesignRequestAttachments (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  DesignRequestId INT           NOT NULL REFERENCES DesignRequests(Id) ON DELETE CASCADE,
  Url             NVARCHAR(500) NOT NULL
);

/* ===== CHAT ===== */
CREATE TABLE ChatThreads (
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId INT       NOT NULL UNIQUE REFERENCES Users(Id),
  CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt  DATETIME  NOT NULL DEFAULT GETDATE()
);

CREATE TABLE ChatMessages (
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  ThreadId   INT           NOT NULL REFERENCES ChatThreads(Id) ON DELETE CASCADE,
  SenderId   INT           NOT NULL REFERENCES Users(Id),
  SenderRole NVARCHAR(30)  NOT NULL, -- Customer / Sales
  Content    NVARCHAR(MAX) NOT NULL,
  CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ===== CMS / LOGS ===== */
CREATE TABLE Contents (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  Title       NVARCHAR(250) NOT NULL,
  Slug        NVARCHAR(250) NOT NULL UNIQUE,
  Type        NVARCHAR(50)  NOT NULL, -- Blog, Guide, News
  Body        NVARCHAR(MAX) NULL,
  CoverUrl    NVARCHAR(500) NULL,
  IsPublished BIT           NOT NULL DEFAULT 0,
  PublishedAt DATETIME2     NULL,
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt   DATETIME2     NULL
);

CREATE TABLE SystemLogs (
  Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
  ActorUserId INT            NULL REFERENCES Users(Id),
  Action      NVARCHAR(100)  NOT NULL,
  Entity      NVARCHAR(100)  NULL,
  EntityId    NVARCHAR(50)   NULL,
  Detail      NVARCHAR(MAX)  NULL,
  CreatedAt   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/* ===== SEED ROLES ===== */
INSERT INTO Roles (Name, Description) VALUES
 (N'Customer', N'Khách hàng'),
 (N'Sales',    N'Nhân viên kinh doanh'),
 (N'Manager',  N'Quản lý catalog / doanh thu'),
 (N'Admin',    N'Quản trị hệ thống');
GO

/* ===== INDEXES ===== */
CREATE INDEX IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IX_Users_IsLocked ON Users(IsLocked);
CREATE INDEX IX_Products_Category ON Products(CategoryId);
CREATE INDEX IX_Products_IsActive ON Products(IsActive) WHERE IsDeleted = 0;
CREATE INDEX IX_Products_Name ON Products(Name);
CREATE INDEX IX_CartItems_ProductId ON CartItems(ProductId);
CREATE INDEX IX_Orders_Customer ON Orders(CustomerId);
CREATE INDEX IX_Orders_Status ON Orders(Status);
CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt DESC);
CREATE INDEX IX_OrderItems_Order ON OrderItems(OrderId);
CREATE INDEX IX_QuotationRequests_Customer ON QuotationRequests(CustomerId);
CREATE INDEX IX_Quotations_Status ON Quotations(Status);
CREATE INDEX IX_DesignRequests_Status ON DesignRequests(Status);
CREATE INDEX IX_ChatMessages_Thread ON ChatMessages(ThreadId);
CREATE INDEX IX_ChatMessages_CreatedAt ON ChatMessages(CreatedAt);
CREATE INDEX IX_SystemLogs_CreatedAt ON SystemLogs(CreatedAt DESC);
CREATE INDEX IX_SystemLogs_Actor ON SystemLogs(ActorUserId);
CREATE INDEX IX_InteriorDesigns_Category ON InteriorDesigns(Category);
CREATE INDEX IX_InteriorDesigns_IsPublished ON InteriorDesigns(IsPublished);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_PasswordReset_Token ON PasswordResetTokens(Token);
GO

/* ===== APP PAGE PERMISSIONS (FE route ACL) ===== */
-- Trang chủ (/) và tổng quan (/admin|/manager|/sales) không nằm trong bảng này.
IF OBJECT_ID(N'dbo.AppPages', N'U') IS NULL
BEGIN
  CREATE TABLE AppPages (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    PageKey     NVARCHAR(120) NOT NULL UNIQUE,
    Name        NVARCHAR(150) NOT NULL,
    Section     NVARCHAR(30)  NOT NULL,
    SortOrder   INT NOT NULL DEFAULT 0,
    IsActive    BIT NOT NULL DEFAULT 1
  );
END

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
  CREATE TABLE RolePermissions (
    RoleId INT NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
    PageId INT NOT NULL REFERENCES AppPages(Id) ON DELETE CASCADE,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PageId)
  );
  CREATE INDEX IX_RolePermissions_PageId ON RolePermissions(PageId);
END
GO


/* ===== SEED ADMIN USER ===== */
-- Password plain: Admin@123
-- Thay @PasswordHash bằng BCrypt hash thật từ BE (ASP.NET Identity / BCrypt.Net).
DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Admin');
DECLARE @PasswordHash NVARCHAR(512) = N'$2a$11$REPLACE_WITH_BCRYPT_HASH_OF_Admin@123';

IF @AdminRoleId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'ngthanhtrung302005@gmail.com')
BEGIN
  INSERT INTO Users (RoleId, Email, PasswordHash, FullName, Phone, IsLocked, IsActive, IsDeleted)
  VALUES (@AdminRoleId, N'ngthanhtrung302005@gmail.com', @PasswordHash, N'trung', N'0352241327', 0, 1, 0);
END
GO


/* ============================================================
   02 PATCH drop Production tables (safe if missing)
   Source: patch_drop_production_tables.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
/* Drop Production module tables (no Production role / FE pages). Safe if missing. */
IF OBJECT_ID(N'dbo.Deliveries', N'U') IS NOT NULL
  DROP TABLE dbo.Deliveries;
GO
IF OBJECT_ID(N'dbo.ProductionOrders', N'U') IS NOT NULL
  DROP TABLE dbo.ProductionOrders;
GO
/* Legacy role row if any */
DELETE FROM RolePermissions WHERE RoleId IN (SELECT Id FROM Roles WHERE Name = N'Production');
DELETE FROM Roles WHERE Name = N'Production';
GO


/* ============================================================
   03 PATCH ChatThreads.UpdatedAt (idempotent)
   Source: patch_ChatThreads_UpdatedAt.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
-- Patch: ChatThreads.UpdatedAt (entity ChatThread + ChatService order by UpdatedAt)
-- Idempotent — chạy được nhiều lần trên DB đã tạo từ create_InteriorStudio.sql cũ.

IF NOT EXISTS (
  SELECT 1
  FROM sys.columns
  WHERE object_id = OBJECT_ID(N'dbo.ChatThreads')
    AND name = 'UpdatedAt'
)
BEGIN
  ALTER TABLE dbo.ChatThreads
    ADD UpdatedAt DATETIME NOT NULL DEFAULT GETDATE();
  PRINT N'Added dbo.ChatThreads.UpdatedAt';
END
ELSE
BEGIN
  PRINT N'dbo.ChatThreads.UpdatedAt already exists — skipped';
END
GO


/* ============================================================
   04 SEED Admin
   Source: seed_admin.sql
   ============================================================ */
GO
/*
  Seed Admin user for InteriorStudio
  Password plain: Admin@123  (change after first login)
  Run against: InteriorStudio on (localdb)\MSSQLLocalDB
*/
USE InteriorStudio;
GO

DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Admin');
DECLARE @PasswordHash NVARCHAR(512) = N'$2a$11$0L3/bCPTOpEAmwzkoNX.Xefen0z0aJBM5bKtM0LjRFWW2WewFGpci';

IF @AdminRoleId IS NULL
BEGIN
  RAISERROR(N'Admin role missing. Run create_InteriorStudio.sql (Roles seed) first.', 16, 1);
  RETURN;
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'ngthanhtrung302005@gmail.com')
BEGIN
  INSERT INTO Users (RoleId, Email, PasswordHash, FullName, Phone, IsLocked, IsActive, IsDeleted)
  VALUES (@AdminRoleId, N'ngthanhtrung302005@gmail.com', @PasswordHash, N'trung', N'0352241327', 0, 1, 0);
END
ELSE
BEGIN
  UPDATE Users
  SET PasswordHash = @PasswordHash,
      FullName = N'trung',
      Phone = N'0352241327',
      RoleId = @AdminRoleId,
      IsLocked = 0,
      IsActive = 1,
      IsDeleted = 0
  WHERE Email = N'ngthanhtrung302005@gmail.com';
END
GO


/* ============================================================
   05 SEED catalog (mock products)
   Source: seed_mock_catalog.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

/* ---- Categories (mock) ---- */
SET IDENTITY_INSERT Categories ON;
MERGE Categories AS t
USING (VALUES
  (1, N'Sofa',   N'Sofa & ghế bành'),
  (2, N'Bàn',    N'Bàn trà / bàn ăn'),
  (3, N'Ghế',    N'Ghế ngồi & công thái học'),
  (4, N'Kệ',     N'Kệ TV / kệ sách'),
  (5, N'Đèn',    N'Chiếu sáng trang trí'),
  (6, N'Giường', N'Phòng ngủ')
) AS s(Id, Name, Description)
ON t.Id = s.Id
WHEN MATCHED THEN UPDATE SET Name = s.Name, Description = s.Description, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Id, Name, Description, IsActive)
  VALUES (s.Id, s.Name, s.Description, 1);
SET IDENTITY_INSERT Categories OFF;
DBCC CHECKIDENT ('Categories', RESEED, 6);

/* ---- Product 1: sync mock + Unsplash ---- */
UPDATE Products SET
  Name = N'Sofa Nordic 3 chỗ',
  Description = N'Sofa gỗ sồi, nệm vải cao cấp, phù hợp phòng khách hiện đại.',
  Price = 12500000,
  MarketPrice = 14800000,
  Stock = 18,
  CategoryId = 1,
  ImageUrl = N'https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=800&q=80',
  IsActive = 1,
  IsDeleted = 0,
  UpdatedAt = SYSDATETIME()
WHERE Id = 1;

MERGE ProductSpecs AS t
USING (SELECT 1 AS ProductId) AS s ON t.ProductId = s.ProductId
WHEN MATCHED THEN UPDATE SET
  Dimensions = N'220 × 90 × 85 cm',
  Material = N'Gỗ sồi FSC, vải linen pha cotton',
  Origin = N'Việt Nam (Bình Dương)',
  Finish = N'Dầu cứng tự nhiên + bọc vải chống bám',
  WeightKg = 48,
  WarrantyMonths = 24
WHEN NOT MATCHED THEN INSERT (ProductId, Dimensions, Material, Origin, Finish, WeightKg, WarrantyMonths)
VALUES (1, N'220 × 90 × 85 cm', N'Gỗ sồi FSC, vải linen pha cotton', N'Việt Nam (Bình Dương)', N'Dầu cứng tự nhiên + bọc vải chống bám', 48, 24);

/* ---- Products 2–6 ---- */
SET IDENTITY_INSERT Products ON;

MERGE Products AS t
USING (VALUES
  (2, 2, N'Bàn trà Oak', N'Mặt bàn gỗ sồi tự nhiên, chân sắt sơn tĩnh điện.',
      3200000, 3900000, 40, N'https://images.unsplash.com/photo-1533090481720-856c6e3c1fdc?w=800&q=80'),
  (3, 3, N'Ghế làm việc Ergonomic', N'Tựa lưng lưới thoáng, nâng đỡ cột sống.',
      4500000, 5200000, 25, N'https://images.unsplash.com/photo-1580480055273-228ff5388ef8?w=800&q=80'),
  (4, 4, N'Kệ TV Walnut', N'Kệ thấp gỗ óc chó, ngăn kéo êm ái.',
      6800000, 7900000, 12, N'https://images.unsplash.com/photo-1595428774223-ef52624120d2?w=800&q=80'),
  (5, 5, N'Đèn sàn Brass', N'Đèn sàn brass ấm, chao vải linen.',
      2100000, 2650000, 30, N'https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=800&q=80'),
  (6, 6, N'Giường Sleepwell 1m6', N'Khung gỗ chắc, đầu giường bọc nệm.',
      9800000, 11500000, 8, N'https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800&q=80')
) AS s(Id, CategoryId, Name, Description, Price, MarketPrice, Stock, ImageUrl)
ON t.Id = s.Id
WHEN MATCHED THEN UPDATE SET
  CategoryId = s.CategoryId, Name = s.Name, Description = s.Description,
  Price = s.Price, MarketPrice = s.MarketPrice, Stock = s.Stock,
  ImageUrl = s.ImageUrl, IsActive = 1, IsDeleted = 0, UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN INSERT (Id, CategoryId, Name, Description, Price, MarketPrice, Stock, ImageUrl, IsActive, IsDeleted)
  VALUES (s.Id, s.CategoryId, s.Name, s.Description, s.Price, s.MarketPrice, s.Stock, s.ImageUrl, 1, 0);

SET IDENTITY_INSERT Products OFF;
DBCC CHECKIDENT ('Products', RESEED, 6);

MERGE ProductSpecs AS t
USING (VALUES
  (2, N'120 × 60 × 42 cm', N'Gỗ sồi nguyên khối, chân thép carbon', N'Việt Nam', N'Phủ matte chống trầy', 18, 12),
  (3, N'65 × 65 × 110–125 cm', N'Lưới PA, khung nhựa PA+GF', N'Hàn Quốc (lắp ráo VN)', N'Khung đen matte', 14, 36),
  (4, N'180 × 40 × 50 cm', N'Gỗ óc chó veneer, MDF E0', N'Việt Nam', N'Dầu cứng mờ', 32, 24),
  (5, N'Ø 45 · cao 160 cm', N'Thân brass, chao linen', N'Đức · VN', N'Brushed brass', 6, 12),
  (6, N'160 × 200 · cao 95 cm', N'Gỗ thông NZ, bọc vải', N'Việt Nam', N'Sơn water-based', 55, 36)
) AS s(ProductId, Dimensions, Material, Origin, Finish, WeightKg, WarrantyMonths)
ON t.ProductId = s.ProductId
WHEN MATCHED THEN UPDATE SET
  Dimensions = s.Dimensions, Material = s.Material, Origin = s.Origin,
  Finish = s.Finish, WeightKg = s.WeightKg, WarrantyMonths = s.WarrantyMonths
WHEN NOT MATCHED THEN INSERT (ProductId, Dimensions, Material, Origin, Finish, WeightKg, WarrantyMonths)
  VALUES (s.ProductId, s.Dimensions, s.Material, s.Origin, s.Finish, s.WeightKg, s.WarrantyMonths);

SELECT Id, Name, LEFT(ImageUrl, 60) AS ImageUrl FROM Products WHERE IsDeleted = 0 ORDER BY Id;
GO


/* ============================================================
   06 FIX VN product texts
   Source: fix_vn_product_seed.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

UPDATE Products SET
  Name = N'Sofa Nordic 3 chỗ',
  Description = N'Sofa gỗ sồi, nệm vải cao cấp, phù hợp phòng khách hiện đại.'
WHERE Id = 1;

UPDATE Categories SET
  Description = N'Sofa & ghế bành'
WHERE Id = 1;

UPDATE ProductSpecs SET
  Dimensions = N'200 x 90 x 85 cm',
  Material = N'Gỗ sồi (Oak)',
  Origin = N'Việt Nam',
  Finish = N'Sơn mờ (Matte)'
WHERE ProductId = 1;

SELECT Id, Name, Description FROM Products WHERE Id = 1;
SELECT Material, Origin, Finish FROM ProductSpecs WHERE ProductId = 1;
GO


/* ============================================================
   07 SEED role permissions / AppPages
   Source: seed_role_permissions.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

/* Catalog trang FE có thể gán quyền (trừ trang chủ / tổng quan) */
IF OBJECT_ID(N'dbo.AppPages', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.AppPages (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    PageKey     NVARCHAR(120) NOT NULL,
    Name        NVARCHAR(150) NOT NULL,
    Section     NVARCHAR(30)  NOT NULL, -- admin | manager | sales
    SortOrder   INT NOT NULL DEFAULT 0,
    IsActive    BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_AppPages_PageKey UNIQUE (PageKey)
  );
END

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.RolePermissions (
    RoleId   INT NOT NULL REFERENCES dbo.Roles(Id) ON DELETE CASCADE,
    PageId   INT NOT NULL REFERENCES dbo.AppPages(Id) ON DELETE CASCADE,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PageId)
  );
  CREATE INDEX IX_RolePermissions_PageId ON dbo.RolePermissions(PageId);
END

/* Seed / sync pages */
MERGE dbo.AppPages AS t
USING (VALUES
  (N'/admin/users',              N'Người dùng',           N'admin',   10),
  (N'/admin/roles',              N'Phân quyền',           N'admin',   20),
  (N'/admin/contents',           N'Nội dung',             N'admin',   30),
  (N'/admin/system-logs',        N'Nhật ký hệ thống',     N'admin',   40),
  (N'/manager/categories',       N'Danh mục',             N'manager', 10),
  (N'/manager/products',         N'Sản phẩm',             N'manager', 20),
  (N'/manager/designs',          N'Concept thiết kế',     N'manager', 30),
  (N'/manager/prices',           N'Bảng giá',             N'manager', 40),
  (N'/manager/best-selling',     N'Bán chạy',             N'manager', 50),
  (N'/manager/revenue',          N'Doanh thu',            N'manager', 60),
  (N'/manager/orders',           N'Đơn hàng (QL)',        N'manager', 70),
  (N'/sales/orders',             N'Đơn hàng',             N'sales',   10),
  (N'/sales/quotations',         N'Báo giá',              N'sales',   20),
  (N'/sales/design-requests',    N'Yêu cầu thiết kế',     N'sales',   40),
  (N'/sales/chat',               N'Chăm sóc khách',       N'sales',   50)
) AS s(PageKey, Name, Section, SortOrder)
ON t.PageKey = s.PageKey
WHEN MATCHED THEN UPDATE SET Name = s.Name, Section = s.Section, SortOrder = s.SortOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT (PageKey, Name, Section, SortOrder, IsActive)
  VALUES (s.PageKey, s.Name, s.Section, s.SortOrder, 1);

/* Trang cũ tách “Duyệt báo giá” → gộp vào hub /sales/quotations */
UPDATE dbo.AppPages
SET IsActive = 0, Name = N'Duyệt báo giá (đã gộp)'
WHERE PageKey = N'/sales/quotation-approval';

/* Gỡ trang Production (không còn role/module) */
UPDATE dbo.AppPages
SET IsActive = 0
WHERE Section = N'production' OR PageKey LIKE N'/production%';

/* Clear & reseed permissions theo quyền hiện tại FE */
DELETE FROM dbo.RolePermissions;

DECLARE @AdminId INT = (SELECT Id FROM Roles WHERE Name = N'Admin');
DECLARE @ManagerId INT = (SELECT Id FROM Roles WHERE Name = N'Manager');
DECLARE @SalesId INT = (SELECT Id FROM Roles WHERE Name = N'Sales');

-- Admin: tất cả trang cấu hình được
IF @AdminId IS NOT NULL
  INSERT INTO dbo.RolePermissions (RoleId, PageId)
  SELECT @AdminId, Id FROM dbo.AppPages WHERE IsActive = 1;

-- Manager: catalog / manager pages
IF @ManagerId IS NOT NULL
  INSERT INTO dbo.RolePermissions (RoleId, PageId)
  SELECT @ManagerId, Id FROM dbo.AppPages WHERE IsActive = 1 AND Section = N'manager';

-- Sales: kinh doanh pages
IF @SalesId IS NOT NULL
  INSERT INTO dbo.RolePermissions (RoleId, PageId)
  SELECT @SalesId, Id FROM dbo.AppPages WHERE IsActive = 1 AND Section = N'sales';

-- Customer: không có back-office pages

SELECT r.Name AS RoleName, p.PageKey, p.Name AS PageName
FROM RolePermissions rp
JOIN Roles r ON r.Id = rp.RoleId
JOIN AppPages p ON p.Id = rp.PageId
ORDER BY r.Name, p.SortOrder, p.PageKey;
GO


/* ============================================================
   08 SEED contents (blog)
   Source: seed_contents.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
/* Seed Contents (Blog/Guide) — UTF-8 */
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Contents WHERE Slug = N'xu-huong-noi-that-2026')
BEGIN
  INSERT INTO Contents (Title, Slug, Type, Body, CoverUrl, IsPublished, PublishedAt, CreatedAt)
  VALUES (
    N'Xu hướng nội thất 2026',
    N'xu-huong-noi-that-2026',
    N'Blog',
    N'<p>Japandi và vật liệu tự nhiên tiếp tục dẫn đầu: gỗ sồi, linen, đồng brass và ánh sáng ấm 3000K.</p>',
    N'https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=900&q=80',
    1,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
  );
END;

IF NOT EXISTS (SELECT 1 FROM Contents WHERE Slug = N'chon-sofa-can-ho-nho')
BEGIN
  INSERT INTO Contents (Title, Slug, Type, Body, CoverUrl, IsPublished, PublishedAt, CreatedAt)
  VALUES (
    N'Cách chọn sofa cho căn hộ nhỏ',
    N'chon-sofa-can-ho-nho',
    N'Guide',
    N'<p>Ưu tiên form thấp, chân cao để sàn thở. Màu be–trắng giúp phòng trông rộng hơn.</p>',
    N'https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?w=900&q=80',
    1,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
  );
END;

IF NOT EXISTS (SELECT 1 FROM Contents WHERE Slug = N'anh-sang-3-lop-trong-nha')
BEGIN
  INSERT INTO Contents (Title, Slug, Type, Body, CoverUrl, IsPublished, PublishedAt, CreatedAt)
  VALUES (
    N'Ánh sáng 3 lớp trong nhà',
    N'anh-sang-3-lop-trong-nha',
    N'Guide',
    N'<p>Ambient, task, accent — kết hợp đèn âm trần, đèn sàn và đèn bàn.</p>',
    N'https://images.unsplash.com/photo-1524758631624-e2822e304c36?w=900&q=80',
    1,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
  );
END;

SELECT Id, Title, Type, IsPublished FROM Contents ORDER BY Id;
GO


/* ============================================================
   09 UPDATE long content bodies
   Source: update_contents_long_body.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
/* Refresh body dài cho 3 bài Contents — chạy được nhiều lần */
SET NOCOUNT ON;

UPDATE Contents
SET Body = N'<p>Năm 2026, xu hướng nội thất tiếp tục nghiêng về không gian “thở được”: ít đồ nhưng chọn đúng chất liệu, ánh sáng ấm và bố cục mở. Japandi — giao thoa tối giản Nhật Bản với sự ấm áp Scandinavian — vẫn là lựa chọn an toàn cho căn hộ đô thị Việt Nam.</p>
<h3>Vật liệu nên ưu tiên</h3>
<ul>
<li>Gỗ sồi / gỗ thông FSC, hoàn thiện dầu cứng hoặc sơn water-based.</li>
<li>Vải linen, cotton, nỉ mềm — tránh bóng nhựa quá lạnh.</li>
<li>Kim loại brushed brass hoặc đen mờ làm điểm nhấn đèn, tay nắm.</li>
<li>Sơn khoáng matte tone ivory, clay, sand thay vì trắng lạnh gắt.</li>
</ul>
<p>Ánh sáng nên giữ khoảng 2700–3000K cho phòng khách và phòng ngủ. Kết hợp ambient (chung), task (đọc/làm việc) và accent (điểm nhấn tường/kệ) để buổi tối không bị phẳng.</p>
<h3>Gợi ý từ Interior Studio</h3>
<p>Bắt đầu từ một concept phòng khách hoặc phòng ngủ trên trang Concept, sau đó gắn các sản phẩm catalog có sẵn (sofa, bàn trà, đèn sàn). Cách này giúp bạn hình dung ngân sách thực tế và đặt hàng theo từng món thay vì mua “full set” ngay từ đầu.</p>
<p>Nếu đang cải tạo căn hộ 40–70 m², hãy giữ lối đi tối thiểu 80–90 cm, ưu tiên sofa chân cao và kệ thấp để sàn vẫn nhìn thấy — phòng sẽ trông rộng hơn rõ rệt.</p>',
    UpdatedAt = SYSUTCDATETIME()
WHERE Slug = N'xu-huong-noi-that-2026';

UPDATE Contents
SET Body = N'<p>Căn hộ nhỏ dễ bị “nuốt” bởi sofa quá to hoặc đặt sát tường. Mục tiêu là chọn form vừa vặn, chân cao để sàn thở, và màu sáng để phản xạ ánh sáng tự nhiên.</p>
<h3>Kích thước &amp; bố cục</h3>
<ul>
<li>Đo tường chính trước: sofa dài khoảng 2/3 bề ngang tường thường cân hơn.</li>
<li>Lối đi quanh sofa tối thiểu 80–90 cm; trước bàn trà còn 35–40 cm để ngồi thoải mái.</li>
<li>Sofa góc chỉ nên dùng khi phòng thực sự có góc chết — nhiều căn studio nên chọn sofa thẳng 2–3 chỗ.</li>
</ul>
<h3>Form &amp; màu</h3>
<p>Ưu tiên lưng thấp–trung bình, chân kim loại hoặc gỗ cao 12–18 cm. Tone be, kem, xám ấm, trắng ngà dễ phối với rèm linen và sàn gỗ. Tránh tay vịn quá dày nếu cửa sổ nhỏ — sẽ làm khung nhìn hẹp lại.</p>
<h3>Checklist nhanh trước khi mua</h3>
<ul>
<li>Đã đo cửa thang máy / cửa chính chưa?</li>
<li>Có chỗ đặt đèn đọc sách cạnh sofa không?</li>
<li>Nệm ngồi có độ lún vừa (không quá mềm khiến khó đứng dậy)?</li>
<li>Vải có chống bám bẩn cơ bản nếu nhà có trẻ / thú cưng?</li>
</ul>
<p>Trên Interior Studio, bạn có thể mở concept phòng khách rồi lọc sản phẩm liên quan để so giá studio với giá thị trường trước khi thêm vào giỏ.</p>',
    UpdatedAt = SYSUTCDATETIME()
WHERE Slug = N'chon-sofa-can-ho-nho';

UPDATE Contents
SET Body = N'<p>Một căn phòng chỉ có một lớp đèn trần thường sẽ hoặc quá tối, hoặc quá phẳng. Công thức “3 lớp ánh sáng” giúp không gian ấm, linh hoạt theo giờ trong ngày và dễ thay đổi mood mà không cần sửa điện lớn.</p>
<h3>1. Ambient — ánh sáng chung</h3>
<p>Đây là nền sáng của phòng: đèn âm trần, đèn chùm nhẹ, hoặc dải LED gián tiếp trên trần/thanh ray. Nên chọn nhiệt độ màu khoảng 3000K cho phòng khách, 2700K cho phòng ngủ. Tránh một bóng công suất cao chiếu thẳng xuống giữa phòng — dễ tạo bóng cứng trên mặt.</p>
<h3>2. Task — ánh sáng làm việc</h3>
<p>Phục vụ đọc sách, nấu ăn, làm việc: đèn bàn, đèn kẹp đầu giường, đèn dưới tủ bếp, đèn treo thấp trên bàn ăn. Ánh sáng task nên đủ rõ nhưng không chói mắt — đặt hơi lệch phía sau vai khi đọc là lý tưởng.</p>
<h3>3. Accent — điểm nhấn</h3>
<p>Làm nổi bức tường, kệ trang trí, tranh hoặc chất liệu gỗ: đèn sàn chiếu lên, spotlight nhỏ, hoặc đèn tường. Accent giúp căn phòng “có chiều sâu” và đẹp hơn trên ảnh cũng như ngoài đời.</p>
<h3>Gợi ý setup thực tế</h3>
<ul>
<li>Phòng khách: 4–6 đèn âm trần dim được + 1 đèn sàn brass + 1 đèn bàn cạnh sofa.</li>
<li>Phòng ngủ: đèn âm trần yếu + 2 đèn đầu giường + rèm 2 lớp để kiểm soát sáng ngày.</li>
<li>Góc làm việc: task lamp 4000K cục bộ, còn lại giữ ấm để mắt đỡ mỏi buổi tối.</li>
</ul>
<p>Khi xem concept trên Interior Studio, hãy chú ý mục thông số chiếu sáng và các sản phẩm đèn liên quan — đó thường là lớp accent/task giúp concept “sống” hơn chỉ với ảnh render.</p>',
    UpdatedAt = SYSUTCDATETIME()
WHERE Slug = N'anh-sang-3-lop-trong-nha';

SELECT Slug, LEN(Body) AS BodyLen FROM Contents WHERE Slug IN (
  N'xu-huong-noi-that-2026',
  N'chon-sofa-can-ho-nho',
  N'anh-sang-3-lop-trong-nha'
);
GO


/* ============================================================
   10 SEED staff Manager + Sales
   Source: seed_staff_accounts.sql
   ============================================================ */
GO
/*
  Seed Manager + Sales demo accounts
  Password plain (both): Cmnr123456@
*/
USE InteriorStudio;
GO

DECLARE @ManagerRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Manager');
DECLARE @SalesRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Sales');
DECLARE @PasswordHash NVARCHAR(512) = N'$2a$11$l66bBDdorxzksvf3VqmxlOEcOFb6La7gqsTDgwLscqDivUMqGCGgG';

IF @ManagerRoleId IS NULL OR @SalesRoleId IS NULL
BEGIN
  RAISERROR(N'Manager/Sales role missing. Run create_InteriorStudio.sql first.', 16, 1);
  RETURN;
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'ngthanhtrung1234@gmail.com')
  INSERT INTO Users (RoleId, Email, PasswordHash, FullName, Phone, IsLocked, IsActive, IsDeleted)
  VALUES (@ManagerRoleId, N'ngthanhtrung1234@gmail.com', @PasswordHash, N'Trung Manager', N'0900001234', 0, 1, 0);
ELSE
  UPDATE Users
  SET RoleId = @ManagerRoleId,
      PasswordHash = @PasswordHash,
      FullName = N'Trung Manager',
      Phone = N'0900001234',
      IsLocked = 0,
      IsActive = 1,
      IsDeleted = 0
  WHERE Email = N'ngthanhtrung1234@gmail.com';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'ngthanhtrung5678@gmail.com')
  INSERT INTO Users (RoleId, Email, PasswordHash, FullName, Phone, IsLocked, IsActive, IsDeleted)
  VALUES (@SalesRoleId, N'ngthanhtrung5678@gmail.com', @PasswordHash, N'Trung Sales', N'0900005678', 0, 1, 0);
ELSE
  UPDATE Users
  SET RoleId = @SalesRoleId,
      PasswordHash = @PasswordHash,
      FullName = N'Trung Sales',
      Phone = N'0900005678',
      IsLocked = 0,
      IsActive = 1,
      IsDeleted = 0
  WHERE Email = N'ngthanhtrung5678@gmail.com';

SELECT u.Id, u.Email, r.Name AS Role, u.FullName
FROM Users u
JOIN Roles r ON r.Id = u.RoleId
WHERE u.Email IN (N'ngthanhtrung1234@gmail.com', N'ngthanhtrung5678@gmail.com');
GO


/* ============================================================
   11 SEED quotations
   Source: seed_quotations.sql
   ============================================================ */
GO
USE InteriorStudio;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

/*
  Seed mẫu QuotationRequests / Quotations (khớp mock FE).
  Chạy sau khi đã có Users (Customer) + Products (Id 1–6).
  Idempotent theo Title + CustomerId.
*/

DECLARE @CustomerRoleId INT = (SELECT TOP 1 Id FROM Roles WHERE Name = N'Customer');
DECLARE @SalesId INT = (SELECT TOP 1 Id FROM Users WHERE Email = N'sales@interior.studio' OR FullName LIKE N'%Sales%');

/* Demo customers nếu chưa có */
IF @CustomerRoleId IS NOT NULL
BEGIN
  IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'an@example.com')
    INSERT INTO Users (RoleId, Email, PasswordHash, FullName, Phone, IsLocked, IsActive, IsDeleted)
    VALUES (@CustomerRoleId, N'an@example.com',
      N'$2a$11$REPLACE_DEMO_HASH', N'Nguyễn Văn An', N'0901234567', 0, 1, 0);

  IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'binh@example.com')
    INSERT INTO Users (RoleId, Email, PasswordHash, FullName, Phone, IsLocked, IsActive, IsDeleted)
    VALUES (@CustomerRoleId, N'binh@example.com',
      N'$2a$11$REPLACE_DEMO_HASH', N'Trần Thị Bình', N'0907654321', 0, 1, 0);
END

DECLARE @AnId INT = (SELECT TOP 1 Id FROM Users WHERE Email = N'an@example.com');
DECLARE @BinhId INT = (SELECT TOP 1 Id FROM Users WHERE Email = N'binh@example.com');

IF @AnId IS NULL OR @BinhId IS NULL
BEGIN
  RAISERROR(N'Cần ít nhất 2 user Customer (an@example.com, binh@example.com) để seed báo giá.', 16, 1);
  RETURN;
END

IF NOT EXISTS (SELECT 1 FROM Products WHERE Id IN (1,2,4,5,6))
BEGIN
  RAISERROR(N'Cần Products Id 1–6 (chạy seed_mock_catalog.sql trước).', 16, 1);
  RETURN;
END

/* ---- Request: phòng khách (Pending) ---- */
DECLARE @Req1 INT;
SELECT @Req1 = Id FROM QuotationRequests
WHERE CustomerId = @AnId AND Title = N'Báo giá phòng khách 20m2';

IF @Req1 IS NULL
BEGIN
  INSERT INTO QuotationRequests (CustomerId, Title, Description, Status, Reply, HandledById, CreatedAt)
  VALUES (@AnId, N'Báo giá phòng khách 20m2',
    N'Sofa Nordic + Bàn trà Oak + Kệ TV Walnut', N'Pending', NULL, NULL, SYSUTCDATETIME());
  SET @Req1 = SCOPE_IDENTITY();
END

MERGE QuotationRequestProducts AS t
USING (VALUES (@Req1, 1), (@Req1, 2), (@Req1, 4)) AS s(QuotationRequestId, ProductId)
ON t.QuotationRequestId = s.QuotationRequestId AND t.ProductId = s.ProductId
WHEN NOT MATCHED THEN INSERT (QuotationRequestId, ProductId) VALUES (s.QuotationRequestId, s.ProductId);

/* ---- Request: phòng ngủ (Replied) ---- */
DECLARE @Req2 INT;
SELECT @Req2 = Id FROM QuotationRequests
WHERE CustomerId = @BinhId AND Title = N'Báo giá phòng ngủ tối giản';

IF @Req2 IS NULL
BEGIN
  INSERT INTO QuotationRequests (CustomerId, Title, Description, Status, Reply, HandledById, CreatedAt)
  VALUES (@BinhId, N'Báo giá phòng ngủ tối giản',
    N'Giường Sleepwell 1m6 + Đèn sàn Brass', N'Replied',
    N'Tổng 11.900.000 ₫ theo catalog hiện tại.', @SalesId, SYSUTCDATETIME());
  SET @Req2 = SCOPE_IDENTITY();
END
ELSE
  UPDATE QuotationRequests SET Status = N'Replied',
    Reply = ISNULL(Reply, N'Tổng 11.900.000 ₫ theo catalog hiện tại.'),
    HandledById = ISNULL(HandledById, @SalesId)
  WHERE Id = @Req2;

MERGE QuotationRequestProducts AS t
USING (VALUES (@Req2, 6), (@Req2, 5)) AS s(QuotationRequestId, ProductId)
ON t.QuotationRequestId = s.QuotationRequestId AND t.ProductId = s.ProductId
WHEN NOT MATCHED THEN INSERT (QuotationRequestId, ProductId) VALUES (s.QuotationRequestId, s.ProductId);

/* ---- Quotation từ Req1: PendingApproval ---- */
DECLARE @Q1 INT;
SELECT @Q1 = Id FROM Quotations WHERE QuotationRequestId = @Req1;

IF @Q1 IS NULL
BEGIN
  INSERT INTO Quotations (QuotationRequestId, CustomerId, Title, Amount, Status, Notes, CreatedById, CreatedAt)
  VALUES (@Req1, @AnId, N'Báo giá phòng khách 20m2', 22500000, N'PendingApproval',
    N'Sofa 12.5M + Bàn 3.2M + Kệ 6.8M = 22.5M', @SalesId, SYSUTCDATETIME());
  SET @Q1 = SCOPE_IDENTITY();
END

MERGE QuotationProducts AS t
USING (VALUES
  (@Q1, 1, 1, CAST(12500000 AS DECIMAL(18,2))),
  (@Q1, 2, 1, CAST(3200000 AS DECIMAL(18,2))),
  (@Q1, 4, 1, CAST(6800000 AS DECIMAL(18,2)))
) AS s(QuotationId, ProductId, Quantity, UnitPrice)
ON t.QuotationId = s.QuotationId AND t.ProductId = s.ProductId
WHEN MATCHED THEN UPDATE SET Quantity = s.Quantity, UnitPrice = s.UnitPrice
WHEN NOT MATCHED THEN INSERT (QuotationId, ProductId, Quantity, UnitPrice)
  VALUES (s.QuotationId, s.ProductId, s.Quantity, s.UnitPrice);

/* ---- Quotation từ Req2: Approved ---- */
DECLARE @Q2 INT;
SELECT @Q2 = Id FROM Quotations WHERE QuotationRequestId = @Req2;

IF @Q2 IS NULL
BEGIN
  INSERT INTO Quotations (QuotationRequestId, CustomerId, Title, Amount, Status, Notes, CreatedById, ApprovedById, CreatedAt)
  VALUES (@Req2, @BinhId, N'Báo giá phòng ngủ tối giản', 11900000, N'Approved',
    N'Giường 9.8M + Đèn 2.1M = 11.9M — đã duyệt', @SalesId, @SalesId, SYSUTCDATETIME());
  SET @Q2 = SCOPE_IDENTITY();
END
ELSE
  UPDATE Quotations SET Status = N'Approved', Amount = 11900000,
    ApprovedById = ISNULL(ApprovedById, @SalesId)
  WHERE Id = @Q2;

MERGE QuotationProducts AS t
USING (VALUES
  (@Q2, 6, 1, CAST(9800000 AS DECIMAL(18,2))),
  (@Q2, 5, 1, CAST(2100000 AS DECIMAL(18,2)))
) AS s(QuotationId, ProductId, Quantity, UnitPrice)
ON t.QuotationId = s.QuotationId AND t.ProductId = s.ProductId
WHEN MATCHED THEN UPDATE SET Quantity = s.Quantity, UnitPrice = s.UnitPrice
WHEN NOT MATCHED THEN INSERT (QuotationId, ProductId, Quantity, UnitPrice)
  VALUES (s.QuotationId, s.ProductId, s.Quantity, s.UnitPrice);

SELECT
  (SELECT COUNT(*) FROM QuotationRequests) AS QuotationRequests,
  (SELECT COUNT(*) FROM Quotations) AS Quotations;
GO


/* ============================================================
   12 FIX quotation unicode
   Source: fix_quotation_unicode.sql
   ============================================================ */
GO
USE InteriorStudio;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
UPDATE Products SET Price = 6800000, UpdatedAt = SYSUTCDATETIME() WHERE Id = 4;
UPDATE Products SET Name = N'Sofa Nordic 3 chỗ' WHERE Id = 1;
UPDATE Products SET Name = N'Bàn trà Oak' WHERE Id = 2;
UPDATE Products SET Name = N'Kệ TV Walnut' WHERE Id = 4;
UPDATE Products SET Name = N'Đèn sàn Brass' WHERE Id = 5;
UPDATE Products SET Name = N'Giường Sleepwell 1m6' WHERE Id = 6;
UPDATE Users SET FullName = N'Nguyễn Văn An' WHERE Email = N'an@example.com';
UPDATE Users SET FullName = N'Trần Thị Bình' WHERE Email = N'binh@example.com';
UPDATE QuotationRequests SET Title = N'Báo giá phòng khách 20m2', Description = N'Sofa Nordic + Bàn trà Oak + Kệ TV Walnut', Reply = NULL WHERE Id = 1;
UPDATE QuotationRequests SET Title = N'Báo giá phòng ngủ tối giản', Description = N'Giường Sleepwell 1m6 + Đèn sàn Brass', Reply = N'Tổng 11.900.000 ₫ theo catalog hiện tại.' WHERE Id = 2;
UPDATE Quotations SET Title = N'Báo giá phòng khách 20m2', Notes = N'Sofa 12.5M + Bàn trà 3.2M + Kệ 6.8M = 22.5M' WHERE Id = 1;
UPDATE Quotations SET Title = N'Báo giá phòng ngủ tối giản', Notes = N'Giường 9.8M + Đèn 2.1M = 11.9M - da duyet' WHERE Id = 2;
SELECT Id, Title FROM QuotationRequests;
SELECT Id, Name, Price FROM Products WHERE Id IN (1,2,4,5,6);
SELECT Id, FullName FROM Users WHERE Email IN (N'an@example.com', N'binh@example.com');
GO


/* ============================================================
   13 RESET admin password hash
   Source: seed_reset_admin_password.sql
   ============================================================ */
GO
/*
  Reset Admin password to Admin@123 (BCrypt)
*/
USE InteriorStudio;
GO
UPDATE Users
SET PasswordHash = N'$2a$11$0L3/bCPTOpEAmwzkoNX.Xefen0z0aJBM5bKtM0LjRFWW2WewFGpci',
    IsLocked = 0,
    IsActive = 1,
    IsDeleted = 0
WHERE Email = N'ngthanhtrung302005@gmail.com';
GO

