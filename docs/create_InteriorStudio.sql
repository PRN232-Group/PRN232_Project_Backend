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
  CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ChatMessages (
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  ThreadId   INT           NOT NULL REFERENCES ChatThreads(Id) ON DELETE CASCADE,
  SenderId   INT           NOT NULL REFERENCES Users(Id),
  SenderRole NVARCHAR(30)  NOT NULL, -- Customer / Sales
  Content    NVARCHAR(MAX) NOT NULL,
  CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ===== PRODUCTION / DELIVERY ===== */
CREATE TABLE ProductionOrders (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  OrderId         INT          NOT NULL REFERENCES Orders(Id),
  Status          NVARCHAR(30) NOT NULL, -- Queued/InProgress/Done/Blocked
  ProgressStatus  NVARCHAR(30) NULL,    -- PENDING/PREPARING/PACKING/SHIPPING/DELIVERED
  ProgressPercent INT          NOT NULL DEFAULT 0 CHECK (ProgressPercent BETWEEN 0 AND 100),
  AssignedToId    INT          NULL REFERENCES Users(Id),
  Deadline        DATE         NULL,
  CreatedAt       DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2    NULL
);

CREATE TABLE Deliveries (
  Id             INT IDENTITY(1,1) PRIMARY KEY,
  OrderId        INT           NOT NULL UNIQUE REFERENCES Orders(Id),
  DeliveryStatus NVARCHAR(30)  NOT NULL, -- Preparing/OutForDelivery/Delivered/Failed
  Note           NVARCHAR(500) NULL,
  DeliveredAt    DATETIME2     NULL,
  UpdatedAt      DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
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
CREATE INDEX IX_ProductionOrders_OrderId ON ProductionOrders(OrderId);
CREATE INDEX IX_ProductionOrders_Status ON ProductionOrders(Status);
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