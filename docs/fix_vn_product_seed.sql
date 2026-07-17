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
