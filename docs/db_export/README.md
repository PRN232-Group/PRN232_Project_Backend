# DB export — nộp database InteriorStudio

| File | Mô tả |
|------|--------|
| `InteriorStudio.bak` | Backup từ LocalDB (máy build) — restore bằng SSMS |
| `InteriorStudio_Full.sql` | Script all-in-one (schema + seed) — cài DB mới từ zero |
| `InteriorStudio_DB_Submit.zip` | Gói nộp gồm `.bak` + `.sql` + `DATABASE_SETUP.md` |

## Restore `.bak`

1. SSMS → Databases → Restore Database…  
2. Device → chọn `InteriorStudio.bak`  
3. Destination database: `InteriorStudio`

## Cài từ script

Xem `../DATABASE_SETUP.md` — chạy `InteriorStudio_Full.sql`.
