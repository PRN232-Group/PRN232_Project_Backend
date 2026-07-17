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
