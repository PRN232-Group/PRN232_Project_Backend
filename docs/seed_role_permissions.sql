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
