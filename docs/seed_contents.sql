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
