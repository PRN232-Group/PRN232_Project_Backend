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
