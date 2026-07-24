# InteriorStudio — Database setup

Chạy **một file** trong SSMS / Azure Data Studio / `sqlcmd` trên SQL Server (LocalDB hoặc instance riêng).

## Script nộp / cài mới

| File | Mô tả |
|------|--------|
| `InteriorStudio_Full.sql` | **All-in-one:** schema + patches + seed (Admin / Staff / catalog / ACL / blog / quotations) |

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i docs/InteriorStudio_Full.sql
```

Hoặc mở `InteriorStudio_Full.sql` trong SSMS → Execute.

## Tài khoản seed (sau khi chạy full script)

| Role | Email | Password |
|------|-------|----------|
| Admin | `ngthanhtrung302005@gmail.com` | `Admin@123` |
| Manager | `ngthanhtrung1234@gmail.com` | `Cmnr123456@` |
| Sales | `ngthanhtrung5678@gmail.com` | `Cmnr123456@` |

## Export / backup để nộp

Thư mục `docs/db_export/`:

- `InteriorStudio.bak` — backup LocalDB (nếu máy có DB chạy)
- `InteriorStudio_Full.sql` — bản copy script all-in-one

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "BACKUP DATABASE [InteriorStudio] TO DISK=N'<path>\\InteriorStudio.bak' WITH FORMAT, INIT, COMPRESSION"
```

## Connection string BE

`ConnectionStrings:InteriorStudioDbConn` trong `appsettings.Development.json`  
(ví dụ: `Server=(localdb)\\MSSQLLocalDB;Database=InteriorStudio;...`).
